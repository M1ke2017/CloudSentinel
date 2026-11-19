# CloudSentinel — final UI deployment

Project: `cloudsentinel-515210` · region: `europe-west1`.

> These commands operate on the user's own local repo and Google Cloud account; they have **not** been executed here. The dashboard is an operations tool: keep it authenticated.

## 1. Verify code and prepare gallery

From the repository root in Developer PowerShell:

```powershell
./Update-ReadmeScreenshots.ps1
dotnet build CloudSentinel.slnx -c Release
dotnet test CloudSentinel.Tests/CloudSentinel.Tests.csproj -c Release
```

## 2. Build and publish the final Blazor UI

```powershell
$project = 'cloudsentinel-515210'
$region = 'europe-west1'
$image = "${region}-docker.pkg.dev/${project}/cloud-sentinel/web:ui-final"

docker build -f Dockerfile.web -t $image .
docker push $image
```

Alternatively, use the fully qualified image reference directly:

```powershell
docker build -f Dockerfile.web -t europe-west1-docker.pkg.dev/cloudsentinel-515210/cloud-sentinel/web:ui-final .
docker push europe-west1-docker.pkg.dev/cloudsentinel-515210/cloud-sentinel/web:ui-final
```

## 3. Update Cloud Run Web without making it public

```powershell
gcloud run services update cloud-sentinel-web --project=cloudsentinel-515210 --region=europe-west1 --image=europe-west1-docker.pkg.dev/cloudsentinel-515210/cloud-sentinel/web:ui-final
```

Do **not** add `--allow-unauthenticated`. The deployment does not automatically resolve the existing IAP/OAuth `403` problem.

> **Blazor Interactive Server:** check page interaction and SignalR connectivity behind your final authentication setup, not just a static HTML response.

## 4. HTTPS/IAP access — outstanding configuration

1. In Cloud Run → `cloud-sentinel-web` → Security, confirm that IAM + IAP settings were actually deployed (not left as pending changes).
2. Complete the Google Auth Platform / IAP OAuth client configuration. The previously observed error was `Empty Google Account OAuth client ID(s)/secret(s)`.
3. Grant the operator the role `roles/iap.httpsResourceAccessor` **on the IAP-protected Cloud Run resource**.
4. Confirm the IAP service agent has `roles/run.invoker` for Web and that the monitoring service retains its required access.
5. Test in a fresh browser session using an authorized Google account. A persistent HTTP 403 means browser access is **not verified**. Do not disable authentication to bypass it.
6. Re-test the monitoring function against the private Web `/health` after IAP is enabled; service-to-service authentication might need adjustment.

The known Cloud Run Web endpoint is:

`https://cloud-sentinel-web-nhos2wmsrq-ew.a.run.app`

It is a service URL, **not a promise of working browser login**.

## 5. Verify execution history

Confirm in Firestore that `monitoring_runs` contains completed entries with start/end timestamps and outcomes. If empty, verify that the version of CloudSentinel.Functions that writes these records has actually been deployed and invoked. Do not infer successful execution solely from the dashboard's Scheduler acceptance message.

## 6. Terraform drift check

A manual deployment updates Cloud Run outside Terraform:

```powershell
cd infra
terraform plan
```

Before applying, align `web_image` (in local ignored `terraform.tfvars`) to the released `web:ui-final` image and review changes to revision routing, tags, IAP and any changes introduced by manual operations. Do not apply a stale `final.tfplan` file.

## 7. Commit final UI and screenshots

```powershell
cd ..
git add README.md DEPLOY.md Update-ReadmeScreenshots.ps1 screenshots/ CloudSentinel.Web/
git diff --cached --name-only
git commit -m "Finalize CloudSentinel UI and screenshot documentation"
git push origin main
git status
```

Do not commit `terraform.tfvars`, Terraform state or plan files, `.json` Google credential keys, or OAuth secrets.

## Final acceptance

- [ ] UI deploy returns successfully; correct revision/image selected
- [ ] Dashboard works in a logged-in browser over HTTPS (IAP), not via localhost proxy
- [ ] Unauthorized account cannot access Web
- [ ] Monitoring service still obtains an authenticated `/health` response
- [ ] `monitoring_runs` contains a completed `Healthy` or `Unhealthy` run
- [ ] Firestore incident history is visible in UI
- [ ] Scheduler is enabled and returns successful execution status
- [ ] Rollback stays disabled until verified end-to-end
- [ ] `terraform plan` reviewed for drift
- [ ] Screenshots render in GitHub README and git working tree is clean
