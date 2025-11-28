$content = Get-Content 'GridContextTests.cs' -Raw

# Fix named parameter patterns
$content = $content -replace 'new GridContext\(\s*correlationId:\s*"([^"]+)",\s*nodeId:', 'new GridContext(correlationId: "$1", operationId: Ulid.NewUlid().ToString(), nodeId:'

# Fix positional 4-param patterns  
$content = $content -replace 'new GridContext\("([^"]+)",\s*"([^"]+)",\s*"([^"]+)",\s*"([^"]+)"\)', 'new GridContext("$1", Ulid.NewUlid().ToString(), "$2", "$3", "$4")'

# Fix variable patterns
$content = $content -replace 'new GridContext\(correlationId,\s*nodeId,\s*studioId,\s*environment\)', 'new GridContext(correlationId, Ulid.NewUlid().ToString(), nodeId, studioId, environment)'
$content = $content -replace 'new GridContext\(correlationId!,\s*nodeId!,\s*studioId!,\s*environment!\)', 'new GridContext(correlationId!, Ulid.NewUlid().ToString(), nodeId!, studioId!, environment!)'

Set-Content 'GridContextTests.cs' -Value $content
