# README — Windows / PowerShell Guide

This README accompanies the blog post **"Adding Observability to .NET Microservices on EKS with ADOT Auto-Instrumentation and Helm."** The blog uses **bash (macOS / Linux)** commands; this file gives the **PowerShell (Windows)** equivalent of each command, in the same order, plus a tool-installation / cluster-creation appendix.

> **PowerShell conventions**
> - Reference environment variables as `$env:VAR` (not `${VAR}`).
> - Set a variable with `$env:VAR="value"` (quotes required) — not `set VAR=value` (that's cmd.exe).
> - Multi-line commands use the backtick ``` continuation (not `\`).
> - Environment variables **do not persist across PowerShell windows** — re-run the "Set environment variables" step if you open a new terminal.

---

## Prerequisites

Before you begin, ensure you have:

- An Amazon EKS cluster having minimum two nodes **if you don't have one yet, see ****[Appendix D](#d-create-the-cluster-with-oidc-enabled-for-irsa)** below to create one with `eksctl`
- AWS Distro for OpenTelemetry EKS Add-On installed
- An kubectl configured
 - Cert-manager installed with a self-signed ClusterIssuer applied
 - An IAM OIDC identity provider associated with the cluster (required for IRSA)
Check with aws eks describe-cluster --name <cluster> --query "cluster.identity.oidc.issuer"; associate one with eksctl utils associate-iam-oidc-provider --cluster <cluster> --approve if missing.
- Helm v3 installed
- AWS CLI v2 configured
- Docker (with buildx) or finch for building images
- An Amazon ECR repository (or permission to create one) per service
- Familiarity with .NET 10 and Kubernetes
- Clone Repo git clone https://gitlab.aws.dev/asramaa/dotcore-adot-instrumentation

### Set environment variables

```powershell
# Edit these for your environment, login to AWS and set the AWS Profile first: 
$env:AWS_PROFILE= <PROFILE>
$env:CLUSTER_NAME="my-cluster"
$env:AWS_REGION="us-east-1"
$env:AWS_ACCOUNT_ID=(aws sts get-caller-identity --query Account --output text)
$ARN = "arn:aws:iam::" + $env:AWS_ACCOUNT_ID + ":policy/ADOTCollectorPolicy"

# Derived — no need to edit:
$env:ECR_REGISTRY="$env:AWS_ACCOUNT_ID.dkr.ecr.$env:AWS_REGION.amazonaws.com"

```



### Enable the IAM OIDC provider (required for IRSA)

```powershell
eksctl utils associate-iam-oidc-provider `
  --cluster $env:CLUSTER_NAME `
  --region $env:AWS_REGION `
  --approve

```

### Create the IAM policy

Write the JSON BOM-free (`Out-File -Encoding utf8` adds a byte-order mark the AWS CLI rejects with `MalformedPolicyDocument: Syntax errors in policy`):

```powershell
$policy = @'
{
  "Version": "2012-10-17",
  "Statement": [{
    "Effect": "Allow",
    "Action": [
      "xray:PutTraceSegments",
      "xray:PutTelemetryRecords",
      "logs:CreateLogGroup",
      "logs:CreateLogStream",
      "logs:PutLogEvents",
      "logs:DescribeLogGroups",
      "logs:DescribeLogStreams",
      "cloudwatch:PutMetricData"
    ],
    "Resource": "*"
  }]
}
'@
[System.IO.File]::WriteAllText("$PWD\adot-collector-policy.json", $policy, (New-Object System.Text.UTF8Encoding($false)))

aws iam create-policy `
  --policy-name ADOTCollectorPolicy `
  --policy-document file://adot-collector-policy.json

# Get the OIDC issuer
$OIDC_URL = (aws eks describe-cluster --name $env:CLUSTER_NAME --region $env:AWS_REGION --query "cluster.identity.oidc.issuer" --output text).Replace("https://","")

# Create trust policy
$trust = @"
{
"Version": "2012-10-17",
"Statement": [{
"Effect": "Allow",
"Principal": {"Federated": "arn:aws:iam::${env:AWS_ACCOUNT_ID}:oidc-provider/$OIDC_URL"},
"Action": "sts:AssumeRoleWithWebIdentity",
"Condition": {"StringLike": {"${OIDC_URL}:sub": "system:serviceaccount:*:adot-collector-collector"}}
}]
}
"@
[System.IO.File]::WriteAllText("$PWD\trust-policy.json", $trust, (New-Object System.Text.UTF8Encoding($false)))

# Create role + attach policy
aws iam create-role --role-name ADOTCollectorRole --assume-role-policy-document file://trust-policy.json
aws iam attach-role-policy --role-name ADOTCollectorRole --policy-arn "arn:aws:iam::${env:AWS_ACCOUNT_ID}:policy/ADOTCollectorPolicy"

# THIS is the ARN you pass to Helm (a ROLE, not a policy):
$ROLE_ARN = "arn:aws:iam::" + $env:AWS_ACCOUNT_ID + ":role/ADOTCollectorRole"

```




## Build and Push Images to ECR (PowerShell)

```powershell
# Authenticate
aws ecr get-login-password --region $env:AWS_REGION | `
  docker login --username AWS --password-stdin $env:ECR_REGISTRY

# Build and push grpc-hello-service (repo root as context; -f points to its Dockerfile)
docker build --platform linux/amd64,linux/arm64  -t grpc-hello-service:latest -f ./grpc-hello-service/Dockerfile .
docker tag grpc-hello-service:latest "$env:ECR_REGISTRY/grpc-hello-service:latest"
docker push "$env:ECR_REGISTRY/grpc-hello-service:latest"

# Build and push hello-world-api (repo root as context; -f points to its Dockerfile)
docker build --platform linux/amd64,linux/arm64  -t hello-world-api:latest -f hello-world-api/Dockerfile .
docker tag hello-world-api:latest "$env:ECR_REGISTRY/hello-world-api:latest"
docker push "$env:ECR_REGISTRY/hello-world-api:latest"

```

---

## Deploy with Helm (PowerShell)

```powershell
helm install grpc-hello-service ./grpc-hello-service/helm/grpc-hello-service `
  -n grpc-hello-service --create-namespace `
  --set image.repository=$env:ECR_REGISTRY/grpc-hello-service `
  --set image.tag=latest `
  --set collector.serviceAccount.roleArn="$ROLE_ARN" `
  --set collector.cloudwatch.region=$env:AWS_REGION `
  --set collector.xray.region=$env:AWS_REGION

helm install hello-world-api ./hello-world-api/helm/hello-world-api `
  -n hello-world-api --create-namespace `
  --set image.repository=$env:ECR_REGISTRY/hello-world-api `
  --set image.tag=latest `
  --set collector.serviceAccount.roleArn="$ROLE_ARN" `
  --set collector.cloudwatch.region=$env:AWS_REGION `
  --set collector.xray.region=$env:AWS_REGION

```

### Verify the Deployment

```powershell
kubectl get pods -n grpc-hello-service
kubectl get pods -n hello-world-api

# Confirm the injected init container (opentelemetry-auto-instrumentation-dotnet)
kubectl get pod -n hello-world-api -l app=hello-world-api `
  -o jsonpath='{.items[0].spec.initContainers[*].name}'
  
 kubectl describe pod -n hello-world-api -l app=hello-world-api | Select-String "Init Containers" -Context 0,10

kubectl get sa adot-collector-collector -n hello-world-api -o jsonpath="{.metadata.annotations.eks\.amazonaws\.com/role-arn}"
```

### Generate Test Traffic

```powershell
# In one terminal, port-forward the REST API:
kubectl port-forward -n hello-world-api svc/hello-world-api 8080:80

# In another terminal, send 50 requests:
1..50 | ForEach-Object {
  try { Invoke-RestMethod -Uri "http://localhost:8080/hello/world" | Out-Null } catch {}
  Write-Host "request $_"
}

# Wait 2-3 min, then check
aws logs describe-log-groups --log-group-name-prefix "/metrics/HelloWorldApi" --region $env:AWS_REGION --query "logGroups[].logGroupName" --output text

```

> **Built-in chaos:** the gRPC backend adds a 2-5 second delay on every 5th request and returns an Internal error on every 10th, so expect elevated p99 latency and a non-zero error rate.

---

## Checking Metrics in Amazon CloudWatch

Open the CloudWatch console (Region = your `AWS_REGION`, e.g. us-west-2) → **Metrics** → **All metrics**, and find the custom namespaces **GrpcHelloService** and **HelloWorldApi**.


### X-Ray Traces

In the CloudWatch console, choose **Application Signals (APM) → Trace Map** to see `hello-world-api` calling `grpc-hello-service`.

### Set Up Alarms (PowerShell)

```powershell
aws cloudwatch put-metric-alarm `
  --alarm-name "HelloWorldApi-HighLatency-P99" `
  --namespace "HelloWorldApi" `
  --metric-name "http.server.request.duration" `
  --extended-statistic p99 `
  --period 300 --evaluation-periods 3 `
  --threshold 2000 `
  --comparison-operator GreaterThanThreshold `
  --region $env:AWS_REGION

```

---

## Cleanup (PowerShell)

```powershell
# 1. Uninstall the Helm releases
helm uninstall hello-world-api -n hello-world-api
helm uninstall grpc-hello-service -n grpc-hello-service

# 2. Delete the IAM policy
# --- IAM Cleanup ---
$POLICY_ARN = "arn:aws:iam::" + $env:AWS_ACCOUNT_ID + ":policy/ADOTCollectorPolicy"
$ROLE_NAME = $ROLE_ARN.Split("/")[-1]

# a. Detach policy from our collector role
aws iam detach-role-policy --role-name $ROLE_NAME --policy-arn $POLICY_ARN 2>$null

# b. Detach policy from any other roles (old eksctl-created ones)
$roles = (aws iam list-entities-for-policy --policy-arn $POLICY_ARN --query "PolicyRoles[].RoleName" --output text 2>$null)
if ($roles) {
  foreach ($R in $roles.Split("`t")) {
    if ($R) { aws iam detach-role-policy --role-name $R --policy-arn $POLICY_ARN }
  }
}

# c. Delete the policy (now detached from everything)
aws iam delete-policy --policy-arn $POLICY_ARN 2>$null

# d. Delete our collector role
aws iam delete-role --role-name $ROLE_NAME 2>$null

Write-Host "IAM cleanup complete: policy + role deleted."

# 3. Remove the ClusterIssuer and the ADOT / cert-manager add-ons
kubectl delete clusterissuer selfsigned-issuer
aws eks delete-addon --cluster-name $env:CLUSTER_NAME --addon-name adot --region $env:AWS_REGION
aws eks delete-addon --cluster-name $env:CLUSTER_NAME --addon-name cert-manager --region $env:AWS_REGION

# 4. (Optional) Delete the ECR repositories and their images
foreach ($REPO in "grpc-hello-service","hello-world-api") {
  aws ecr delete-repository --repository-name $REPO --force --region $env:AWS_REGION
}

# 5. Delete the CloudWatch log groups
aws logs delete-log-group --log-group-name /metrics/HelloWorldApi --region $env:AWS_REGION
aws logs delete-log-group --log-group-name /metrics/GrpcHelloService --region $env:AWS_REGION

```

## Troubleshooting (PowerShell)

**Metrics never reach CloudWatch — **`AccessDenied`** on **`logs:PutLogEvents`**.** The collector is using the node instance role, not IRSA — usually the wrong service-account name. It must be `adot-collector-collector`. Rebind (see "Grant Collector IAM Permissions") and restart the collectors. Verify the role is injected:

```powershell
$POD = (kubectl get pods -n hello-world-api -o name | Select-String "adot-collector-collector" | Select-Object -First 1).ToString().Replace("pod/","")
kubectl get pod -n hello-world-api $POD -o jsonpath="{.spec.containers[0].env[?(@.name=='AWS_ROLE_ARN')].value}"

```

**Pods stuck Pending / no nodes.**

```powershell
eksctl get nodegroup --cluster $env:CLUSTER_NAME --region $env:AWS_REGION
eksctl scale nodegroup --cluster $env:CLUSTER_NAME --name standard-nodes --nodes 2 --nodes-min 2 --nodes-max 4 --region $env:AWS_REGION

```

**Pods Running but no init container.** Injection happens at pod-creation time; recreate pods once the operator is healthy:

```powershell
kubectl rollout restart deployment -n grpc-hello-service grpc-hello-service
kubectl rollout restart deployment -n hello-world-api hello-world-api

```

`MalformedPolicyDocument: Syntax errors in policy`**.** `Out-File -Encoding utf8` on Windows PowerShell 5.x adds a UTF-8 BOM the AWS CLI rejects. Use the `[System.IO.File]::WriteAllText(..., UTF8Encoding($false))` here-strings shown above, or pass the JSON inline.

**Console shows no data.** Confirm the CloudWatch Region selector (top-right) matches your deployment — the console defaults to whichever Region you last used.

---

## Appendix — Install the CLI Tools and Create a Cluster (PowerShell)

### A. Install eksctl

```powershell
choco install eksctl          # or: scoop install eksctl
eksctl version

```

### B. Install kubectl

```powershell
choco install kubernetes-cli  # or: scoop install kubectl
kubectl version --client

```

### C. Install Helm

```powershell
choco install kubernetes-helm # or: scoop install helm / winget install Helm.Helm
helm version

```

### D. Create the cluster with OIDC enabled for IRSA

```powershell
eksctl create cluster --name $env:CLUSTER_NAME --region $env:AWS_REGION --version 1.31 --nodegroup-name standard-nodes --node-type m5.large ` `
--nodes 2 --nodes-min 2 --nodes-max 4 --managed --with-oidc

```
> `--with-oidc` creates the IAM OIDC provider that IRSA depends on. `--nodes-min 2` prevents the node group from scaling to zero and stranding the cluster.

If the cluster already exists without OIDC:

```powershell
eksctl utils associate-iam-oidc-provider --cluster $env:CLUSTER_NAME --region $env:AWS_REGION --approve

```

### Verify kubectl can reach the cluster

If you just created the cluster with `eksctl`, kubectl is already configured. Otherwise, run:

```powershell
aws eks update-kubeconfig --name $env:CLUSTER_NAME --region $env:AWS_REGION

```

Confirm connectivity:

```powershell
kubectl get nodes

```

You should see your nodes in `Ready` state.

### Cluster setup

Ensure the node group has at least two nodes (a minimum of 0 can strand the cluster):

```powershell
eksctl scale nodegroup --cluster $env:CLUSTER_NAME --name standard-nodes --nodes 2 --nodes-min 2 --region $env:AWS_REGION

```

---

## E. Create the ECR repository (PowerShell)

```powershell
foreach ($REPO in "grpc-hello-service","hello-world-api") {
  aws ecr create-repository --repository-name $REPO --region $env:AWS_REGION
}
```

## F. Install cert-manager

```powershell
aws eks create-addon --cluster-name $env:CLUSTER_NAME --addon-name cert-manager --region $env:AWS_REGION

### Wait ~2 min, then verify: 
aws eks describe-addon --cluster-name $env:CLUSTER_NAME --addon-name cert-manager --region $env:AWS_REGION --query "addon.status"
kubectl get pods -n cert-manager

```

Create and apply the self-signed ClusterIssuer (write the file BOM-free — `Out-File -Encoding utf8` on Windows PowerShell 5.x adds a byte-order mark some tools reject):

```powershell
$issuer = @'
apiVersion: cert-manager.io/v1
kind: ClusterIssuer
metadata:
  name: selfsigned-issuer
spec:
  selfSigned: {}
'@
[System.IO.File]::WriteAllText("$PWD\cluster-issuer.yaml", $issuer, (New-Object System.Text.UTF8Encoding($false)))

kubectl apply -f cluster-issuer.yaml

```

##G. Install the ADOT EKS add-on

```powershell
aws eks create-addon `
  --cluster-name $env:CLUSTER_NAME `
  --addon-name adot `
  --region $env:AWS_REGION

```

```
---
