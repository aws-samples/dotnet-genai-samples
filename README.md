# dotnet-adot-instrumentation

Two .NET 10 microservices demonstrating AWS Distro for OpenTelemetry (ADOT) auto-instrumentation on EKS:

- `grpc-hello-service` — a gRPC backend service
- `hello-world-api` — a REST API that calls the gRPC service

## Prerequisites

- Docker
- AWS CLI configured with ECR access
- Helm 3
- `kubectl` configured for your EKS cluster

## Build and Push Docker Images

Set your ECR registry and region:

```bash
export AWS_REGION=us-east-1 
export CLUSTER_NAME=my-cluster 
export AWS_ACCOUNT_ID=$(aws sts get-caller-identity --query Account --output text)
export ECR_REGISTRY=${AWS_ACCOUNT_ID}.dkr.ecr.${AWS_REGION}.amazonaws.com
```

Authenticate Docker with ECR:

```bash
aws ecr get-login-password --region ${AWS_REGION} | docker login --username AWS --password-stdin ${ECR_REGISTRY}
```

### grpc-hello-service

```bash
docker build -t grpc-hello-service:latest ./grpc-hello-service
docker tag grpc-hello-service:latest ${ECR_REGISTRY}/grpc-hello-service:latest
docker push ${ECR_REGISTRY}/grpc-hello-service:latest
```

### hello-world-api

The hello-world-api Dockerfile expects the build context at the repo root (it copies the proto from `grpc-hello-service/protos/`):

```bash
docker build -t hello-world-api:latest -f hello-world-api/Dockerfile .
docker tag hello-world-api:latest ${ECR_REGISTRY}/hello-world-api:latest
docker push ${ECR_REGISTRY}/hello-world-api:latest
```

## Deploy to EKS with Helm

### Create IAM Policy

```bash
cat > adot-collector-policy.json <<'EOF'
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
EOF
# Create the iam policy.
aws iam create-policy  --policy-name ADOTCollectorPolicy  --policy-document file://adot-collector-policy.json
export POLICY_ARN="arn:aws:iam::${ACCOUNT_ID}:policy/ADOTCollectorPolicy"
```

### Create IAM Role for serivce account

Ensure Cert-manager is installed with a self-signed ClusterIssuer applied on your cluster

```bash

export OIDC_PROVIDER=$(aws eks describe-cluster --name ${CLUSTER_NAME} \
  --query "cluster.identity.oidc.issuer" --output text | sed -e "s/^https:\/\///")

cat > trust-policy.json <<EOF
{
  "Version": "2012-10-17",
  "Statement": [{
    "Effect": "Allow",
    "Principal": {
      "Federated": "arn:aws:iam::${ACCOUNT_ID}:oidc-provider/${OIDC_PROVIDER}"
    },
    "Action": "sts:AssumeRoleWithWebIdentity",
    "Condition": {
      "StringEquals": {
        "${OIDC_PROVIDER}:aud": "sts.amazonaws.com",
        "${OIDC_PROVIDER}:sub": [
          "system:serviceaccount:hello-world-api:adot-collector-collector",
          "system:serviceaccount:grpc-hello-service:adot-collector-collector"
        ]
      }
    }
  }]
}
EOF

aws iam create-role \
  --role-name ADOTCollectorRole \
  --assume-role-policy-document file://trust-policy.json

aws iam attach-role-policy \
  --role-name ADOTCollectorRole \
  --policy-arn ${POLICY_ARN}

export ROLE_ARN=$(aws iam get-role --role-name ADOTCollectorRole --query "Role.Arn" --output text)
```

### Install grpc-hello-service

```bash
helm install grpc-hello-service ./grpc-hello-service/helm/grpc-hello-service \
  -n grpc-hello-service --create-namespace \
  --set image.repository=${ECR_REGISTRY}/grpc-hello-service \
  --set image.tag=latest \
  --set collector.cloudwatch.region=$AWS_REGION \
  --set collector.serviceAccount.roleArn=$ROLE_ARN \
  --set collector.xray.region=$AWS_REGION
```

### Install hello-world-api

```bash
helm install hello-world-api ./hello-world-api/helm/hello-world-api \
  -n hello-world-api --create-namespace \
  --set image.repository=${ECR_REGISTRY}/hello-world-api \
  --set image.tag=latest \
  --set collector.cloudwatch.region=$AWS_REGION \
  --set collector.serviceAccount.roleArn=$ROLE_ARN \
  --set collector.xray.region=$AWS_REGION
```

### Upgrade an existing release

```bash
helm upgrade grpc-hello-service ./grpc-hello-service/helm/grpc-hello-service \
  --set image.tag=<new-tag>

helm upgrade hello-world-api ./hello-world-api/helm/hello-world-api \
  --set image.tag=<new-tag>
```

### Uninstall

```bash
helm uninstall hello-world-api -n hello-world-api
helm uninstall grpc-hello-service -n grpc-hello-service
```

## Verify the deployment

```bash
kubectl get pods -n grpc-hello-service
kubectl get pods -n hello-world-api
```
