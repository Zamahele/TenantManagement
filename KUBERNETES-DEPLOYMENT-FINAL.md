# Property Management System - Kubernetes Deployment (Final Working Configuration)

## 🎯 **Verified Working Configuration**

This configuration has been tested and verified to work correctly on Kubernetes with HTTPS access.

## 📋 **Prerequisites**
- Kubernetes cluster (tested with Rancher Desktop)
- kubectl configured and connected
- GitHub Container Registry access
- SSL certificate file (`aspnetapp.pfx`)

## 🔑 **Required Credentials**
- **GitHub Email**: Set via environment variable `$env:GITHUB_EMAIL` or replace `YOUR_GITHUB_EMAIL`
- **GitHub Token**: Set via environment variable `$env:GITHUB_TOKEN` or replace `YOUR_GITHUB_TOKEN`
- **Database Password**: `Your_password123` (change for production)
- **SSL Certificate**: `aspnetapp.pfx` (in project root)

## 🚀 **Working Deployment Configuration**

### Key Configuration Elements:

1. **Docker Image**: `ghcr.io/zamahele/tenantmanagement:latest` ✅
2. **Image Pull Secret**: GHCR authentication configured ✅
3. **SSL Certificate**: Mounted from `aspnetapp.pfx` ✅
4. **Database Seeding**: **DISABLED** (prevents crashes) ✅
5. **HTTPS Configuration**: Both HTTP:80 and HTTPS:443 exposed ✅

### Critical Environment Variables:
```yaml
- name: ASPNETCORE_ENVIRONMENT
  value: "Production"
- name: ASPNETCORE_URLS  
  value: "http://+:80;https://+:443"
- name: ASPNETCORE_FORWARDEDHEADERS_ENABLED
  value: "true"
- name: ASPNETCORE_HTTPS_PORT
  value: ""
- name: EnableDatabaseSeeding
  value: "false"  # CRITICAL: Must be false to prevent crashes
```

### Volume Mounts:
```yaml
volumeMounts:
- name: https-cert
  mountPath: /https
  readOnly: true
- name: app-config
  mountPath: /app/appsettings.Production.json
  subPath: appsettings.Production.json
  readOnly: true
- name: app-logs
  mountPath: /app/logs
```

## 📁 **Deployment Files Structure**
```
k8s/
├── 01-namespace.yaml       # Namespace: property-management
├── 02-secrets.yaml         # SQL Server and app secrets
├── 03-configmap.yaml       # Application configuration
├── 04-storage.yaml         # Persistent volumes for SQL and logs
├── 05-sql-server.yaml      # SQL Server 2022 deployment
├── 06-web-app.yaml         # Web application deployment
├── 07-services.yaml        # LoadBalancer services
└── 08-ingress.yaml         # Optional ingress (not required)
```

## 🔧 **Deployment Commands (Manual)**

```bash
# 1. Create namespace and base resources
kubectl apply -f k8s/01-namespace.yaml
kubectl apply -f k8s/02-secrets.yaml
kubectl apply -f k8s/03-configmap.yaml
kubectl apply -f k8s/04-storage.yaml

# 2. Create SSL certificate secret
kubectl create secret generic https-cert \
  --from-file=aspnetapp.pfx=aspnetapp.pfx \
  -n property-management

# 3. Create GHCR authentication secret
# Set your GitHub credentials first:
# export GITHUB_EMAIL="your-github-email@example.com"
# export GITHUB_TOKEN="your_github_token_here"
kubectl create secret docker-registry ghcr-secret \
  --docker-server=ghcr.io \
  --docker-username=$GITHUB_EMAIL \
  --docker-password="$GITHUB_TOKEN" \
  --docker-email=$GITHUB_EMAIL \
  -n property-management

# 4. Deploy SQL Server
kubectl apply -f k8s/05-sql-server.yaml

# 5. Wait for SQL Server to be ready
kubectl wait --for=condition=available --timeout=300s deployment/sql-server -n property-management

# 6. Deploy web application and services
kubectl apply -f k8s/06-web-app.yaml
kubectl apply -f k8s/07-services.yaml

# 7. Wait for web application to be ready
kubectl wait --for=condition=available --timeout=300s deployment/property-management-web -n property-management
```

## 🌐 **Permanent Access (No Port Forwarding Required)**

### NodePort Configuration ✅
The service is configured with NodePort for permanent access without port forwarding:

```yaml
type: NodePort
ports:
- port: 443
  targetPort: 443
  nodePort: 30443  # HTTPS
- port: 80
  targetPort: 80
  nodePort: 30080  # HTTP
```

### Direct Access URLs:

**HTTPS (Recommended):**
- **URL**: `https://localhost:30443`
- **Username**: `Admin`
- **Password**: `01Pa$$w0rd2025#`
- **Certificate**: Click "Advanced" → "Proceed to localhost (unsafe)"

**HTTP (Redirects to HTTPS):**
- **URL**: `http://localhost:30080` (will redirect to HTTPS)

### Legacy Port Forward Method:
```bash
# If you prefer port forwarding (not needed with NodePort)
kubectl port-forward svc/property-management-service 8443:443 -n property-management
# Then access: https://localhost:8443
```

## 📊 **Verification Commands**

```bash
# Check deployment status
kubectl get all -n property-management

# Check pod logs
kubectl logs -f deployment/property-management-web -n property-management

# Check SQL Server logs
kubectl logs -f deployment/sql-server -n property-management

# Test HTTPS connection
curl -k -I https://localhost:8443/Tenants/Login

# Scale application
kubectl scale deployment property-management-web --replicas=3 -n property-management
```

## ⚠️ **Critical Success Factors**

1. **Database Seeding MUST be disabled**: `EnableDatabaseSeeding: "false"`
2. **GHCR Authentication**: Correct email and token required (use environment variables)
3. **SSL Certificate**: `aspnetapp.pfx` must exist in project root
4. **SQL Server Ready**: Always wait for SQL Server before deploying web app
5. **Port Forward**: Use port 8443 for HTTPS, accept browser certificate warnings

## 🧹 **Clean Deployment**

To completely reset and redeploy:
```bash
# Delete everything
kubectl delete namespace property-management

# Wait for cleanup
sleep 10

# Redeploy using the commands above
```

## 📈 **Production Considerations**

- **External LoadBalancer**: Configure proper external IP instead of port-forward
- **SSL Certificates**: Use proper CA-signed certificates in production
- **Secrets Management**: Use Azure Key Vault or AWS Secrets Manager
- **Database**: Consider managed database service instead of in-cluster SQL Server
- **Monitoring**: Deploy Prometheus/Grafana for monitoring
- **Backup**: Configure automated backups for persistent volumes

## ✅ **Tested and Verified**

This configuration has been tested with:
- ✅ Complete clean deployment
- ✅ HTTPS access working
- ✅ Database connectivity
- ✅ Application functionality
- ✅ Port forwarding
- ✅ Pod scaling

**Last Verified**: August 25, 2025
**Kubernetes Version**: v1.31.6+k3s1 (Rancher Desktop)
**Container Runtime**: Docker 27.3.1