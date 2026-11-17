#!/usr/bin/env bash
# Builds both apps as ARM64 container images (no Docker needed: the .NET SDK pushes
# straight to ECR), then deploys infra/ecs-fargate.yaml.
#
#   infra/deploy-ecs.sh                         # storefront open to your current public IP only
#   ALLOWED_CIDR=198.51.100.0/24 infra/deploy-ecs.sh
#
# Uses the standard AWS_PROFILE / AWS_REGION environment variables (default region us-east-1).
set -euo pipefail
cd "$(dirname "$0")/.."

export AWS_REGION="${AWS_REGION:-us-east-1}"
TAG="${IMAGE_TAG:-$(date +%Y%m%d%H%M%S)}"
ALLOWED_CIDR="${ALLOWED_CIDR:-$(curl -fsS https://checkip.amazonaws.com | tr -d '[:space:]')/32}"

echo "==> Deploying to account $(aws sts get-caller-identity --query Account --output text), region $AWS_REGION"
echo "==> ECR repositories"
aws cloudformation deploy --stack-name dotnet-shop-ecr --template-file infra/ecr.yaml --no-fail-on-empty-changeset

ACCOUNT="$(aws sts get-caller-identity --query Account --output text)"
REGISTRY="$ACCOUNT.dkr.ecr.$AWS_REGION.amazonaws.com"

# The SDK's container publishing reads registry credentials from these variables.
export SDK_CONTAINER_REGISTRY_UNAME=AWS
SDK_CONTAINER_REGISTRY_PWORD="$(aws ecr get-login-password)"
export SDK_CONTAINER_REGISTRY_PWORD

for app in Web Api; do
  repo="dotnet-shop/$(echo "$app" | tr '[:upper:]' '[:lower:]')"
  echo "==> Building and pushing $REGISTRY/$repo:$TAG"
  dotnet publish "src/Shop.$app/Shop.$app.csproj" -c Release -r linux-arm64 -t:PublishContainer \
    -p:ContainerRegistry="$REGISTRY" -p:ContainerRepository="$repo" -p:ContainerImageTag="$TAG" -v q
done

echo "==> ECS stack (storefront allowed from $ALLOWED_CIDR)"
aws cloudformation deploy --stack-name dotnet-shop-ecs --template-file infra/ecs-fargate.yaml \
  --capabilities CAPABILITY_IAM \
  --parameter-overrides ImageTag="$TAG" AllowedCidr="$ALLOWED_CIDR"

aws cloudformation describe-stacks --stack-name dotnet-shop-ecs \
  --query "Stacks[0].Outputs[?OutputKey=='StorefrontUrl'].OutputValue" --output text
