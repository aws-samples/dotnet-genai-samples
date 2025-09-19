#!/bin/bash

# Build and Package Script for Hotel Reservations Lambda
# This script uses SAM to build the lambda and creates a deployment zip file

set -e

echo "Building Hotel Reservations Lambda using SAM..."

# Clean previous builds
echo "Cleaning previous builds..."
rm -rf .aws-sam/
mkdir -p deployment/

# Build using SAM
echo "Running SAM build..."
sam build --use-container

# Check if build was successful
if [ ! -d ".aws-sam/build/HotelReservationFunction" ]; then
    echo "Error: SAM build failed or build directory not found"
    exit 1
fi

echo "SAM build completed successfully"

# Create deployment package
echo "Creating deployment package..."
cd .aws-sam/build/HotelReservationFunction

# Create zip file with all lambda artifacts
zip -r ../../../deployment/reservations-lambda-package.zip . -x "*.git*" "*.DS_Store*"

cd ../../../

echo "Deployment package created: deployment/reservations-lambda-package.zip"

# Display package contents
echo "Package contents:"
unzip -l deployment/reservations-lambda-package.zip | head -20

# Display package size
echo "Package size:"
ls -lh deployment/reservations-lambda-package.zip

echo "Build and packaging completed successfully!"