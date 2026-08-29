# LibraryPro Database Backup Strategy

## Overview

This document outlines the comprehensive backup strategy for LibraryPro's database and related data assets.

## Backup Components

### 1. Database Backups

#### SQL Server (On-Premises)

**Automated Daily Backups:**
```sql
-- Create a maintenance plan or SQL Agent job
BACKUP DATABASE LibraryProDb 
TO DISK = 'C:\Backups\LibraryProDb_Daily_' + CONVERT(VARCHAR, GETDATE(), 112) + '.bak'
WITH FORMAT, COMPRESSION, STATS = 10;
```

**Weekly Full Backups:**
- Schedule: Every Sunday at 2:00 AM
- Retention: 4 weeks

**Daily Differential Backups:**
- Schedule: Daily at 3:00 AM (Monday-Saturday)
- Retention: 1 week

**Transaction Log Backups:**
- Schedule: Every 15 minutes
- Retention: 2 days

#### Azure SQL Database

**Automated Backups:**
- Enable in Azure Portal
- Retention period: 7-35 days (configurable)
- Point-in-time restore: Up to 35 days

**Manual Backups:**
```bash
# Using Azure CLI
az sql db export \
  --name LibraryProDb \
  --resource-group librarypro-rg \
  --server librarypro-server \
  --storage-key <storage-key> \
  --storage-uri https://storageaccount.blob.core.windows.net/backups/LibraryProDb.bacpac
```

### 2. File System Backups

#### Uploaded Images

**Directories to Backup:**
- `wwwroot/images/books` - Book cover images
- `wwwroot/images/members` - Member profile photos

**Backup Strategy:**
- Daily incremental backups
- Weekly full backups
- Retention: 90 days

**Backup Script (Windows):**
```powershell
# Daily backup script
$source = "C:\inetpub\wwwroot\LibraryPro\wwwroot\images"
$destination = "D:\Backups\Images_$(Get-Date -Format 'yyyyMMdd')"
Copy-Item -Path $source -Destination $destination -Recurse -Force
Compress-Archive -Path $destination -DestinationPath "$destination.zip"
```

### 3. Configuration Backups

**Files to Backup:**
- `appsettings.Production.json`
- `appsettings.json`
- Any custom configuration files

**Backup Strategy:**
- Manual backup before any configuration changes
- Version control (Git) for configuration files

## Backup Schedule

| Backup Type | Frequency | Schedule | Retention |
|-------------|-----------|----------|-----------|
| Full Database | Weekly | Sunday 2:00 AM | 4 weeks |
| Differential Database | Daily | 3:00 AM | 1 week |
| Transaction Log | Every 15 min | Continuous | 2 days |
| Image Files | Daily | 1:00 AM | 90 days |
| Configuration | On Change | Manual | Indefinite |

## Backup Storage

### Local Storage

**Primary Location:**
- Path: `D:\Backups\LibraryPro\`
- Disk space: Minimum 500GB
- RAID configuration: RAID 10 for performance and redundancy

### Offsite/Cloud Storage

**Azure Blob Storage:**
- Container: `librarypro-backups`
- Redundancy: Geo-redundant storage (GRS)
- Lifecycle management: Move to cool storage after 30 days, archive after 90 days

**AWS S3 (Alternative):**
- Bucket: `librarypro-backups`
- Storage class: Standard for recent backups, Glacier for archives

## Restore Procedures

### Database Restore

#### SQL Server

**Full Restore:**
```sql
RESTORE DATABASE LibraryProDb 
FROM DISK = 'C:\Backups\LibraryProDb_Daily_20240827.bak'
WITH REPLACE, STATS = 10;
```

**Point-in-Time Restore:**
```sql
RESTORE DATABASE LibraryProDb 
FROM DISK = 'C:\Backups\LibraryProDb_Daily_20240827.bak'
WITH NORECOVERY, STOPAT = '2024-08-27 14:30:00';
RESTORE DATABASE LibraryProDb 
FROM DISK = 'C:\Backups\LibraryProDb_Log_20240827_1430.trn'
WITH RECOVERY;
```

#### Azure SQL Database

**Point-in-Time Restore:**
```bash
az sql db restore \
  --dest-database LibraryProDb_Restored \
  --name LibraryProDb \
  --resource-group librarypro-rg \
  --server librarypro-server \
  --time "2024-08-27T14:30:00Z"
```

### File Restore

**Image Files:**
```powershell
# Restore from backup
$source = "D:\Backups\Images_20240827.zip"
$destination = "C:\inetpub\wwwroot\LibraryPro\wwwroot\images"
Expand-Archive -Path $source -DestinationPath $destination -Force
```

## Disaster Recovery

### Recovery Time Objective (RTO)

- **Critical systems:** 4 hours
- **Non-critical systems:** 24 hours

### Recovery Point Objective (RPO)

- **Database:** 15 minutes (transaction log backups)
- **File system:** 24 hours (daily backups)

### Disaster Recovery Plan

1. **Assessment Phase (0-1 hour):**
   - Identify scope of disaster
   - Declare disaster recovery mode
   - Notify stakeholders

2. **Recovery Phase (1-4 hours):**
   - Restore latest full database backup
   - Apply transaction logs
   - Restore file system backups
   - Verify application functionality

3. **Validation Phase (4-6 hours):**
   - Test critical user flows
   - Verify data integrity
   - Performance testing

4. **Cutover Phase (6-8 hours):**
   - Switch DNS if needed
   - Update load balancer configuration
   - Monitor for issues

## Monitoring and Alerts

### Backup Monitoring

**Metrics to Monitor:**
- Backup success/failure status
- Backup duration
- Backup size
- Storage capacity

**Alert Thresholds:**
- Backup failure: Immediate alert
- Backup duration > 2 hours: Warning
- Storage capacity < 20%: Critical alert

### Monitoring Tools

- **SQL Server:** SQL Agent alerts, Azure Monitor
- **File System:** Windows Event Log, custom monitoring scripts
- **Cloud Storage:** Azure Monitor, AWS CloudWatch

## Testing

### Backup Testing

**Monthly Testing:**
- Restore database to test environment
- Verify data integrity
- Test application functionality with restored data

**Quarterly Testing:**
- Full disaster recovery drill
- Test all recovery procedures
- Document lessons learned

### Test Procedure

1. **Select a random backup from 30 days ago**
2. **Restore to test environment**
3. **Run data validation queries**
4. **Test critical application features**
5. **Document any issues found**
6. **Update procedures if needed**

## Security Considerations

### Backup Encryption

**Database Backups:**
```sql
-- Enable TDE (Transparent Data Encryption)
CREATE MASTER KEY ENCRYPTION BY PASSWORD = 'StrongPassword';
CREATE CERTIFICATE LibraryProCert WITH SUBJECT = 'LibraryPro Database';
CREATE DATABASE ENCRYPTION KEY
WITH ALGORITHM = AES_256
ENCRYPTION BY SERVER CERTIFICATE LibraryProCert;
ALTER DATABASE LibraryProDb SET ENCRYPTION ON;
```

**File Backups:**
- Encrypt backup files using BitLocker or similar
- Use encrypted storage for cloud backups

### Access Control

- Restrict backup file access to authorized personnel only
- Use role-based access control for backup operations
- Audit all backup and restore operations

## Documentation

### Backup Logs

Maintain logs of:
- All backup operations
- All restore operations
- Any backup failures
- Test results

### Change Management

Document any changes to:
- Backup schedules
- Backup procedures
- Retention policies
- Storage locations

## Compliance

### Data Retention

- **Audit logs:** 90 days (as per GDPR requirements)
- **Transaction logs:** 2 days
- **Full数据库 backups:** 4 weeks
- **Image files:** 90 days

### Data Privacy

- Ensure backups comply with GDPR and other privacy regulations
- Implement data anonymization for test restores
- Secure backup storage with appropriate access controls

## Contact Information

**Primary Contact:**
- Database Administrator: dba@librarypro.com
- System Administrator: sysadmin@librarypro.com

**Emergency Contacts:**
- On-call DBA: +1-XXX-XXX-XXXX
- On-call SysAdmin: +1-XXX-XXX-XXXX

## Appendix

### Sample Backup Script (PowerShell)

```powershell
# LibraryPro Database Backup Script
# Run as scheduled task daily

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupPath = "D:\Backups\LibraryProDb_$timestamp.bak"
$logPath = "D:\Backups\backup_$timestamp.log"

try {
    Write-Output "Starting backup at $timestamp" | Out-File $logPath
    
    # Backup database
    sqlcmd -S localhost -E -Q "BACKUP DATABASE LibraryProDb TO DISK = '$backupPath' WITH FORMAT, COMPRESSION" >> $logPath
    
    # Compress backup
    Compress-Archive -Path $backupPath -DestinationPath "$backupPath.zip"
    Remove-Item $backupPath
    
    # Upload to cloud storage
    # Add your cloud storage upload logic here
    
    Write-Output "Backup completed successfully" | Out-File $logPath -Append
}
catch {
    Write-Output "Backup failed: $_" | Out-File $logPath -Append
    # Send alert email
    Send-MailMessage -To "dba@librarypro.com" -Subject "LibraryPro Backup Failed" -Body $_ -From "backup@librarypro.com"
}
```

### Sample Restore Script (PowerShell)

```powershell
# LibraryPro Database Restore Script

$backupFile = "D:\Backups\LibraryProDb_20240827.zip"
$tempPath = "C:\Temp\LibraryProRestore"
$logPath = "D:\Backups\restore_$(Get-Date -Format 'yyyyMMdd_HHmmss').log"

try {
    Write-Output "Starting restore" | Out-File $logPath
    
    # Extract backup
    Expand-Archive -Path $backupFile -DestinationPath $tempPath -Force
    
    # Restore database
    $bakFile = Get-ChildItem $tempPath -Filter "*.bak" | Select-Object -First 1
    sqlcmd -S localhost -E -Q "RESTORE DATABASE LibraryProDb FROM DISK = '$($bakFile.FullName)' WITH REPLACE" >> $logPath
    
    # Cleanup
    Remove-Item $tempPath -Recurse -Force
    
    Write-Output "Restore completed successfully" | Out-File $logPath -Append
}
catch {
    Write-Output "Restore failed: $_" | Out-File $logPath -Append
    # Send alert email
    Send-MailMessage -To "dba@librarypro.com" -Subject "LibraryPro Restore Failed" -Body $_ -From "backup@librarypro.com"
}
```
