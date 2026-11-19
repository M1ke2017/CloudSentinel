
variable "project_id" {
  description = "Google Cloud Project ID"
  type        = string
}

variable "region" {
  description = "Google Cloud region"
  type        = string
  default     = "europe-west1"
}

variable "web_image" {
  description = "Docker image for CloudSentinel.Web"
  type        = string
}

variable "function_image" {
  description = "Docker image for CloudSentinel.Functions"
  type        = string
}
