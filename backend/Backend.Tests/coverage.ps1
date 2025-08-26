# coverage.ps1
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura /p:CoverletOutput=TestResults/coverage.cobertura.xml

reportgenerator -reports:TestResults/coverage.cobertura.xml `
                -targetdir:coveragereport `
                -reporttypes:Html

start "$PWD/coveragereport/index.html"