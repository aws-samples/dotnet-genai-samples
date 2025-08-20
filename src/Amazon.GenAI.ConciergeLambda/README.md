# Amazon.GenAI.ConciergeLambda

A serverless concierge service built with AWS Lambda and Bedrock Agent that handles cab booking requests using AWS Powertools for .NET.

## Features

- **Bedrock Agent Integration**: Function resolver for Amazon Bedrock Agent
- **Cab Booking Management**: Create and manage cab bookings with DynamoDB storage
- **Event Notifications**: SNS notifications for booking events
- **Structured Logging**: AWS Lambda Powertools for .NET logging and tracing

## Architecture

- **Lambda Function**: Processes Bedrock Agent requests for cab bookings
- **DynamoDB Table**: Stores cab booking records
- **SNS Topic**: Publishes booking events
- **Bedrock Agent**: Natural language interface for booking requests

## Available Tools

- `CreateCabBooking`: Creates a new cab booking for a guest with specified dates

## Prerequisites

- AWS CLI configured
- .NET 8 SDK

## Environment Variables

- `DYNAMODB_TABLE_NAME`: DynamoDB table for cab bookings
- `SNS_TOPIC_ARN`: SNS topic for booking notifications
- `AWS_REGION`: AWS region for services

## Project Structure

```
├── src/Amazon.GenAI.ConciergeLambda/
│   ├── Function.cs              # Main Lambda handler
│   ├── Services/
│   │   ├── ConciergeService.cs  # Business logic
│   │   └── IConciergeService.cs # Service interface
│   └── Models/
│       ├── CabBooking.cs        # Booking model
│       └── BookingStatus.cs     # Status enum
└── template.yaml                # CloudFormation template
```
