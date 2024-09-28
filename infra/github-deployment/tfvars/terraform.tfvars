gh_uai_name = "uai-gh-biotrackr"
storage_account_name = "storbiotrackrprodtfstate"
tf_state_rg_name = "rg-biotrackr-tfstate-prod"
identity_rg_name = "rg-biotrackr-identity-prod"
location = "australiaeast"
tags = {
  environment = "Production"
  owner = "willvelida"
  application = "biotrackr"
}
container_names = [
  "biotrackrcore-tfstate",
  "biotrack-auth-tfstate"
]