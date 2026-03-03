# dotnet-adot-instrumentation

Two .NET 8 microservices demonstrating AWS Distro for OpenTelemetry (ADOT) auto-instrumentation on EKS:

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
export AWS_ACCOUNT_ID=123456789012
export AWS_REGION=us-west-2
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

### Install grpc-hello-service

```bash
helm install grpc-hello-service ./grpc-hello-service/helm/grpc-hello-service \
  --set image.repository=${ECR_REGISTRY}/grpc-hello-service \
  --set image.tag=latest
```

### Install hello-world-api

```bash
helm install hello-world-api ./hello-world-api/helm/hello-world-api \
  --set image.repository=${ECR_REGISTRY}/hello-world-api \
  --set image.tag=latest
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
helm uninstall grpc-hello-service
helm uninstall hello-world-api
```

## Verify the deployment

```bash
kubectl get pods -n grpc-hello-service
kubectl get pods -n hello-world-api
```
