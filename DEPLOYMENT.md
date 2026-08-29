# LibraryPro Deployment Guide

## Prerequisites

- .NET 10.0 SDK
- SQL Server 2019 or later
- SendGrid account (for email services)
- SSL certificate (for production HTTPS)

## Environment Configuration

### Development Environment

The application uses `appsettings.json` and `appsettings.Development.json` for local development.

### Production Environment

1. **Copy and configure `appsettings.Production.json`:**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=LibraryProDb;User Id=YOUR_USER;Password=YOUR_PASSWORD;TrustServerCertificate=False;MultipleActiveResultSets=true"
  },
  "EmailSettings": {
    "ApiKey": "YOUR_SENDGRID_API_KEY",
    "FromEmail": "noreply@yourdomain.com",
    "FromName": "LibraryPro"
  },
  "Jwt": {
    "Issuer": "https://yourdomain.com",
    "Audience": "LibraryProAPI",
    "Key": "YOUR_SECURE_JWT_KEY_MINIMUM_32_CHARACTERS"
  }
}
```

2. **Update the following values:**
   - Database connection string
   - SendGrid API key
   - JWT issuer and key
   - Email settings

## Database Setup

### Option 1: SQL Server

1. Create a new SQL Server database named `LibraryProDb`
2. Run the following command to apply migrations:

```bash
dotnet ef database update --connection "YourConnectionString"
```

### Option 2: Azure SQL Database

1. Create an Azure SQL Database
2. Update connection string in `appsettings.Production.json`
3. Run migrations:

```bash
dotnet ef database update --connection "YourAzureConnectionString"
```

## Deployment Methods

### Method 1: Traditional Deployment

1. **Publish the application:**

```bash
dotnet publish -c Release -o ./publish
```

2. **Copy the `publish` folder to your server**

3. **Configure IIS:**
   - Install ASP.NET Core Hosting Bundle
   - Create a new website in IIS
   - Point to the `publish` folder
   - Configure HTTPS with your SSL certificate

4. **Set up Application Pool:**
   - .NET CLR Version: No Managed Code
   - Pipeline Mode: Integrated
   - Identity: ApplicationPoolIdentity (or specific service account)

### Method 2: Docker Deployment

1. **Build the Docker image:**

```bash
docker build -t librarypro:latest .
```

2. **Run the container:**

```bash
docker run -d -p 80:80 -p 443:443 \
  -e ConnectionStrings__DefaultConnection="YourConnectionString" \
  -e EmailSettings__ApiKey="YourSendGridKey" \
  -e Jwt__Key="YourJwtKey" \
  -v /path/to/images:/app/wwwroot/images/books \
  --name librarypro \
  librarypro:latest
```

3. **Configure reverse proxy (nginx/Apache) for SSL termination**

### Method 3: Azure App Service

1. **Create a new Azure App Service**

2. **Configure application settings:**
   - `ConnectionStrings__DefaultConnection`: Your Azure SQL connection string
   - `EmailSettings__ApiKey`: Your SendGrid API key
   - `Jwt__Key`: Your secure JWT key

3. **Deploy using Visual Studio or Azure CLI:**

```bash
az webapp up --name librarypro-app --resource-group librarypro-rg --sku B1
```

## Post-Deployment Configuration

### 1. Create Admin User

After first deployment, log in with the default admin credentials:
- Email: `admin@librarypro.com`
- Password: `Admin@1234`

**Important:** Change the default password immediately after first login.

### 2. Configure Email Settings

1. Update SendGrid API key in production settings
2. Test email functionality by triggering a loan notification

### 3. Configure Image Upload Path

Ensure the `wwwroot/images/books` directory has write permissions for the application pool identity.

### 4. Set Up Scheduled Tasks

Configure the following background services:
- Email Background Service (built-in)
- Audit Log Cleanup Service (built-in, runs automatically)

## Health Monitoring

### Health Check Endpoint

Access `/health` endpoint to monitor application health:
- Healthy: Database connection successful
- Unhealthy: Database connection failed

### Recommended Monitoring Tools

- Application Insights (Azure)
- Prometheus + Grafana
- New Relic
- Datadog

## Security Checklist

- [ ] Change default admin password
- [ ] Configure HTTPS with valid SSL certificate
- [ ] Update JWT key to a secure value
- [ ] Configure database connection with least-privilege account
- [ ] Enable firewall rules to restrict database access
- [ ] Set up regular database backups
- [ ] Configure CORS if needed
- [ ] Review and update security headers
- [ ] Enable rate limiting on API endpoints
- [ ] Set up API key authentication for API endpoints

## Backup Strategy

### Database Backups

**For SQL Server:**
```sql
BACKUP DATABASE LibraryProDb TO DISK = 'C:\Backups\LibraryProDb.bak' WITH FORMAT
```

**For Azure SQL Database:**
- Enable automated backups in Azure Portal
- Configure retention period (7-35 days)
- Set up point-in-time restore

### File Backups

Backup the following directories:
- `wwwroot/images/books` (uploaded book images)
- `wwwroot/images/members` (uploaded member photos)

### Application Configuration

Backup:
- `appsettings.Production.json`
- Any custom configuration files

## Troubleshooting

### Common Issues

**1. Database Connection Failed**
- Check connection string in `appsettings.Production.json`
- Verify database server is accessible
- Check firewall rules

**2. Email Not Sending**
- Verify SendGrid API key
- Check email settings configuration
- Review application logs

**3. Image Upload Failing**
- Verify write permissions on `wwwroot/images/books`
- Check file size limits in `appsettings.Production.json`
- Ensure allowed extensions are configured

**4. Health Check Returns Unhealthy**
- Check database connectivity
- Review application logs
- Verify database migrations are applied

## Performance Optimization

1. **Enable Response Compression** (already configured in Program.cs)
2. **Configure Static File Caching**
3. **Use CDN for static assets**
4. **Optimize database queries**
5. **Enable connection pooling**
6. **Configure session state appropriately**

## Scaling Considerations

### Horizontal Scaling

- Use load balancer (Azure Load Balancer, Nginx, HAProxy)
- Deploy multiple instances behind load balancer
- Configure sticky sessions if needed
- Use distributed cache (Redis) for session state

### Vertical Scaling

- Increase CPU and memory resources
- Optimize database performance
- Use read replicas for database queries

## Support and Maintenance

### Regular Maintenance Tasks

- Weekly: Review application logs
- Monthly: Apply security updates
- Monthly: Review and optimize database performance
- Quarterly: Review and update dependencies
- Quarterly: Test disaster recovery procedures

### Update Procedure

1. Backup database and configuration
2. Deploy new version to staging environment
3. Test thoroughly in staging
4. Deploy to production with zero-downtime deployment
5. Monitor for issues
6. Roll back if necessary

## Contact Information

For deployment issues or questions:
- Development Team: dev@librarypro.com
- Support: support@librarypro.com
