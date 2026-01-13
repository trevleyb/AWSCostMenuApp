# Setting up the SSO Access

## Create the folder
`mkdir -p ~/.aws`

## Create the config file
`cat > ~/.aws/config << 'EOF'`
`[sso-session <name>]`
`sso_start_url = <sso start URL>`
`sso_region = ap-southeast-2`
`sso_registration_scopes = sso:account:access`

[profile master]
`sso_session = <session name>`
`sso_account_id = <account number>`
`sso_role_name = <role>`
`region = us-east-1`
`EOF`


## Verify the file
cat ~/.aws/config

## Sign in to create the token
aws sso login --profile master

## Test It
aws sts get-caller-identity --profile master

