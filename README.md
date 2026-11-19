## Google Cloud Deployment — November 2026

**Deployment status:** Deployed and operational with limitations.

**Google Cloud project:** `cloudsentinel-515210`  
**Region:** `europe-west1`

### Verified features

- Cloud Run Web and Monitoring services deployed.
- Blazor panel and `/health` endpoint operational.
- Cloud Scheduler automatically invokes monitoring every 5 minutes.
- Firestore stores detected incidents.
- Docker images are hosted in Artifact Registry.
- Terraform provisions the cloud infrastructure.
- GitHub Actions builds and tests the application.
- Manual Cloud Run traffic rollback successfully tested.

### Known limitation

Automated Traffic Rollback is implemented but has not been successfully verified in Google Cloud. It remains disabled using `REMEDIATION_ENABLED=false`.

The system currently performs monitoring and incident recording without automatic traffic modification.

### Operational configuration

Cloud Scheduler: `cloud-sentinel-check` — enabled.  
Monitoring interval: 5 minutes.  
Web revision receiving traffic: `cloud-sentinel-web-00002-tdg` (last reported; verify before operational use).  
Verified rollback revision: `cloud-sentinel-web-00001-l4n`.

### Outstanding technical work

- Reconcile Terraform with manually updated Cloud Run settings and traffic configuration.
- Verify automated Traffic Rollback in a controlled environment.
- Review automated deployment and image publication in GitHub Actions.