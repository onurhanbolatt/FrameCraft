#!/bin/bash

echo "Initializing LocalStack S3..."

# Create the bucket
awslocal s3 mb s3://framecraft-dev

# Set bucket policy for public read (development only)
awslocal s3api put-bucket-policy --bucket framecraft-dev --policy '{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Sid": "PublicReadGetObject",
      "Effect": "Allow",
      "Principal": "*",
      "Action": "s3:GetObject",
      "Resource": "arn:aws:s3:::framecraft-dev/*"
    }
  ]
}'

# Enable CORS
awslocal s3api put-bucket-cors --bucket framecraft-dev --cors-configuration '{
  "CORSRules": [
    {
      "AllowedHeaders": ["*"],
      "AllowedMethods": ["GET", "PUT", "POST", "DELETE", "HEAD"],
      "AllowedOrigins": ["*"],
      "ExposeHeaders": ["ETag"]
    }
  ]
}'

echo "LocalStack S3 initialization complete!"
echo "Bucket 'framecraft-dev' created successfully."
