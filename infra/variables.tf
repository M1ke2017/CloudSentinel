
variable "project_id" {
  description = "Identyfikator projektu Google Cloud"
  type        = string
}

variable "region" {
  description = "Region infrastruktury"
  type        = string
  default     = "europe-west1"
}

variable "web_image" {
  description = "Obraz Docker aplikacji Blazor"
  type        = string
}

variable "function_image" {
  description = "Obraz Docker funkcji monitorujacej"
  type        = string
}

resource "google_cloud_run_v2_service" "web" {
  name                = "cloud-sentinel-web"
  project             = var.project_id
  location            = var.region
  deletion_protection = false
  ingress             = "INGRESS_TRAFFIC_ALL"

  template {
    scaling {
      min_instance_count = 0
      max_instance_count = 1
    }

    containers {
      image = var.web_image

      ports {
        container_port = 8080
      }
    }
  }

  depends_on = [google_project_service.apis]
}

resource "google_cloud_run_v2_service" "monitoring" {
  name                = "cloud-sentinel-monitoring"
  project             = var.project_id
  location            = var.region
  deletion_protection = false
  ingress             = "INGRESS_TRAFFIC_ALL"

  template {
    scaling {
      min_instance_count = 0
      max_instance_count = 1

    }
    service_account = google_service_account.monitoring.email
    containers {
      image = var.function_image
      env {
        name  = "GOOGLE_CLOUD_PROJECT"
        value = var.project_id
      }
      ports {
        container_port = 8080
      }
    }
  }

  depends_on = [google_project_service.apis]
}
