# Kubernetes Deployment for Property Management System

This file contains all the Kubernetes manifests needed to deploy the Property Management System to a Kubernetes cluster.

## Prerequisites

- Kubernetes cluster (v1.20+)  
- kubectl configured
- Docker image built and pushed to registry
- Ingress controller installed

## Quick Deploy

Create individual YAML files with the content below, then run:

```bash
kubectl apply -f .
```

## 1. Namespace

```yaml
apiVersion: v1
kind: Namespace
metadata:
  name: property-management
  labels:
    app: property-management
```

## 2. Secrets

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: sql-server-secret
  namespace: property-management
type: Opaque
data:
  SA_PASSWORD: WW91cl9wYXNzd29yZDEyMw==  # Your_password123
---
apiVersion: v1
kind: Secret
metadata:
  name: app-secrets
  namespace: property-management
type: Opaque
data:
  ASPNETCORE_ENVIRONMENT: UHJvZHVjdGlvbg==  # Production
```

## 3. ConfigMap

```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: app-config
  namespace: property-management
data:
  ConnectionStrings__DefaultConnection: "Server=sql-server-service,1433;Database=PropertyManagementDb;User=sa;Password=Your_password123;MultipleActiveResultSets=true;TrustServerCertificate=True"
  UtilityRates__WaterPerLiter: "0.02"
  UtilityRates__ElectricityPerKwh: "1.50"
```

## 4. Storage

```yaml
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: sql-server-pvc
  namespace: property-management
spec:
  accessModes:
    - ReadWriteOnce
  resources:
    requests:
      storage: 20Gi
---
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: app-logs-pvc
  namespace: property-management
spec:
  accessModes:
    - ReadWriteOnce
  resources:
    requests:
      storage: 5Gi
```

## 5. SQL Server

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: sql-server
  namespace: property-management
spec:
  replicas: 1
  selector:
    matchLabels:
      app: sql-server
  template:
    metadata:
      labels:
        app: sql-server
    spec:
      containers:
      - name: sql-server
        image: mcr.microsoft.com/mssql/server:2022-latest
        ports:
        - containerPort: 1433
        env:
        - name: ACCEPT_EULA
          value: "Y"
        - name: SA_PASSWORD
          valueFrom:
            secretKeyRef:
              name: sql-server-secret
              key: SA_PASSWORD
        - name: MSSQL_PID
          value: "Express"
        volumeMounts:
        - name: sql-storage
          mountPath: /var/opt/mssql
        resources:
          requests:
            memory: "2Gi"
            cpu: "500m"
          limits:
            memory: "4Gi"
            cpu: "1"
      volumes:
      - name: sql-storage
        persistentVolumeClaim:
          claimName: sql-server-pvc
```

## 6. Web Application

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: property-management-web
  namespace: property-management
spec:
  replicas: 2
  selector:
    matchLabels:
      app: property-management-web
  template:
    metadata:
      labels:
        app: property-management-web
    spec:
      initContainers:
      - name: wait-for-db
        image: busybox:1.28
        command: ['sh', '-c', 'until nc -z sql-server-service 1433; do echo waiting for db; sleep 2; done;']
      containers:
      - name: web-app
        image: your-registry/property-management:latest  # UPDATE THIS
        ports:
        - containerPort: 80
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: ConnectionStrings__DefaultConnection
          valueFrom:
            configMapKeyRef:
              name: app-config
              key: ConnectionStrings__DefaultConnection
        - name: UtilityRates__WaterPerLiter
          valueFrom:
            configMapKeyRef:
              name: app-config
              key: UtilityRates__WaterPerLiter
        - name: UtilityRates__ElectricityPerKwh
          valueFrom:
            configMapKeyRef:
              name: app-config
              key: UtilityRates__ElectricityPerKwh
        volumeMounts:
        - name: app-logs
          mountPath: /app/logs
        resources:
          requests:
            memory: "512Mi"
            cpu: "250m"
          limits:
            memory: "1Gi"
            cpu: "500m"
        livenessProbe:
          httpGet:
            path: /
            port: 80
          initialDelaySeconds: 30
          periodSeconds: 30
        readinessProbe:
          httpGet:
            path: /
            port: 80
          initialDelaySeconds: 5
          periodSeconds: 10
      volumes:
      - name: app-logs
        persistentVolumeClaim:
          claimName: app-logs-pvc
```

## 7. Services

```yaml
apiVersion: v1
kind: Service
metadata:
  name: sql-server-service
  namespace: property-management
spec:
  selector:
    app: sql-server
  ports:
    - port: 1433
      targetPort: 1433
  type: ClusterIP
---
apiVersion: v1
kind: Service
metadata:
  name: property-management-service
  namespace: property-management
spec:
  selector:
    app: property-management-web
  ports:
    - port: 80
      targetPort: 80
  type: LoadBalancer
```

## 8. Ingress (Optional)

```yaml
apiVersion: networking.k8s.io/v1
kind: Ingress
metadata:
  name: property-management-ingress
  namespace: property-management
  annotations:
    nginx.ingress.kubernetes.io/rewrite-target: /
spec:
  rules:
  - host: property-management.your-domain.com  # UPDATE THIS
    http:
      paths:
      - path: /
        pathType: Prefix
        backend:
          service:
            name: property-management-service
            port:
              number: 80
```

## Deployment Steps

1. **Build and push Docker image**:
```bash
docker build -t your-registry/property-management:latest -f PropertyManagement.Web/Dockerfile .
docker push your-registry/property-management:latest
```

2. **Update the image reference** in the web deployment above

3. **Deploy to Kubernetes**:
```bash
# Apply all manifests
kubectl apply -f 01-namespace.yaml
kubectl apply -f 02-secrets.yaml  
kubectl apply -f 03-configmap.yaml
kubectl apply -f 04-storage.yaml
kubectl apply -f 05-sql-server.yaml
kubectl apply -f 06-web-app.yaml
kubectl apply -f 07-services.yaml
kubectl apply -f 08-ingress.yaml  # Optional
```

4. **Verify deployment**:
```bash
kubectl get all -n property-management
kubectl logs -f deployment/property-management-web -n property-management
```

## Access the Application

- With LoadBalancer: Get external IP with `kubectl get svc -n property-management`
- With Ingress: Access via your configured domain
- Default login: Admin / 01Pa$$w0rd2025#

## Important Notes

- Update the Docker image reference in the web deployment
- Change passwords in secrets for production
- Configure proper storage classes for your cluster
- Set up SSL certificates for production use
- Adjust resource requests/limits based on your cluster