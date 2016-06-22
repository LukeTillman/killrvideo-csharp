<#
  .DESCRIPTION
  Pull the latest from the projects that are git subtrees.
#>

git subtree pull --prefix lib/killrvideo-docker-common git@github.com:KillrVideo/killrvideo-docker-common.git master --squash
git subtree pull --prefix lib/killrvideo-service-protos git@github.com:KillrVideo/killrvideo-service-protos.git master --squash