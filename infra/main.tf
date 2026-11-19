
terraform {
  required_version = ">= 1.10.0"

  required_providers {
    google = {
      source  = "hashicorp/google"
      version = "~> 7.0"
    }
  }
}

provider "google" {
  project = var.project_id
  region  = var.region
}

locals {
  required_apis = toset([
    "run.googleapis.com",
    "cloudscheduler.googleapis.com",
    "artifactregistry.googleapis.com",
    "firestore.googleapis.com",
    "monitoring.googleapis.com",
    "iam.googleapis.com",
    "cloudbuild.googleapis.com"
  ])
}

resource "google_project_service" "apis" {
  for_each = local.required_apis

  project            = var.project_id
  service            = each.value
  disable_on_destroy = false
}

resource "google_artifact_registry_repository" "docker" {
  project       = var.project_id
  location      = var.region
  repository_id = "cloud-sentinel"
  description   = "CloudSentinel Docker images"
  format        = "DOCKER"

  depends_on = [google_project_service.apis]
}

resource "google_firestore_database" "default" {
  project     = var.project_id
  name        = "(default)"
  location_id = var.region
  type        = "FIRESTORE_NATIVE"

  depends_on = [google_project_service.apis]
}

resource "google_service_account" "scheduler" {
  project      = var.project_id
  account_id   = "cloud-sentinel-scheduler"
  display_name = "CloudSentinel Scheduler"

  depends_on = [google_project_service.apis]
}

resource "google_cloud_run_v2_service_iam_member" "scheduler_invoker" {
  project  = var.project_id
  location = var.region
  name     = google_cloud_run_v2_service.monitoring.name
  role     = "roles/run.invoker"
  member   = "serviceAccount:${google_service_account.scheduler.email}"
}

resource "google_cloud_scheduler_job" "monitoring" {
  project   = var.project_id
  region    = var.region
  name      = "cloud-sentinel-check"
  schedule  = "*/5 * * * *"
  time_zone = "Etc/UTC"

  http_target {
    http_method = "GET"
    uri         = google_cloud_run_v2_service.monitoring.uri

    oidc_token {
      service_account_email = google_service_account.scheduler.email
      audience              = google_cloud_run_v2_service.monitoring.uri
    }
  }

  depends_on = [
    google_project_service.apis,
    google_cloud_run_v2_service_iam_member.scheduler_invoker
  ]
}

resource "google_service_account" "web" {
  project      = var.project_id
  account_id   = "cloud-sentinel-web"
  display_name = "CloudSentinel Web"
}

resource "google_service_account" "monitoring" {
  project      = var.project_id
  account_id   = "cloud-sentinel-monitor"
  display_name = "CloudSentinel Monitoring"
}

resource "google_project_iam_member" "web_firestore" {
  project = var.project_id
  role    = "roles/datastore.user"
  member  = "serviceAccount:${google_service_account.web.email}"
}

resource "google_project_iam_member" "monitoring_roles" {
  for_each = toset([
    "roles/run.developer",
    "roles/monitoring.viewer",
    "roles/datastore.user"
  ])

  project = var.project_id
  role    = each.value
  member  = "serviceAccount:${google_service_account.monitoring.email}"
}

