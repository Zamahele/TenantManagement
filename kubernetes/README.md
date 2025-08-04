# Kubernetes Deployment Guide for Property Management System

This directory contains Kubernetes manifests for deploying the Property Management System to a Kubernetes cluster.

## Prerequisites

- Kubernetes cluster (v1.20+)
- kubectl configured to access your cluster
- Docker images built and pushed to a container registry
- Ingress controller installed (e.g., NGINX)

## Quick Deploy

```bash
# Apply all manifests
kubectl apply -f .

# Or apply in order
kubectl apply -f 01-namespace.yaml
kubectl apply -f 02-secrets.yaml
kubectl apply -f 03-configmap.yaml
kubectl apply -f 04-persistent-volume.yaml
kubectl apply -f 05-sql-server.yaml
kubectl apply -f 06-web-app.yaml
kubectl apply -f 07-services.yaml
kubectl apply -f 08-ingress.yaml
```

## Configuration

Before deploying, update the following:

1. **Secrets**: Update database passwords in `02-secrets.yaml`
2. **ConfigMap**: Modify app settings in `03-configmap.yaml`
3. **Ingress**: Update domain name in `08-ingress.yaml`
4. **Images**: Update container image references in deployment files

## Files Overview

- `01-namespace.yaml` - Creates the property-management namespace
- `02-secrets.yaml` - Database credentials and certificates
- `03-configmap.yaml` - Application configuration
- `04-persistent-volume.yaml` - Storage for SQL Server
- `05-sql-server.yaml` - SQL Server deployment
- `06-web-app.yaml` - Property Management web application
- `07-services.yaml` - Kubernetes services for networking
- `08-ingress.yaml` - External access configuration

## Access

After deployment:
- Application: https://property-management.your-domain.com
- Internal SQL Server: sql-server-service:1433 (within cluster)

## Monitoring

The application includes:
- Prometheus metrics at `/metrics`
- Health checks at `/health`
- Structured logging with Serilog