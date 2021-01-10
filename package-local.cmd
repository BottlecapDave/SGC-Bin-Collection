dotnet restore ./src --force

dotnet build ./src/ -c Release -o out /p:Version=0.1.0-alpha

XCOPY "src/SouthGloucestershireBinCollection/out" "C:\nuget.local" /s