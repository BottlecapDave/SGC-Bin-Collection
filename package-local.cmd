dotnet restore ./src --force

dotnet build ./src/Bottlecap.Net.Bots/ -c Release -o out /p:Version=0.1.0-alpha
dotnet pack ./src/Bottlecap.Net.Bots/ -c Release -o out /p:Version=0.1.0-alpha

dotnet build ./src/Bottlecap.Net.Bots.Alexa/ -c Release -o out /p:Version=0.6.0-alpha
dotnet pack ./src/Bottlecap.Net.Bots.Alexa/ -c Release -o out /p:Version=0.6.0-alpha

XCOPY "src/Bottlecap.Net.Bots/out" "C:\nuget.local" /s
XCOPY "src/Bottlecap.Net.Bots.Alexa/out" "C:\nuget.local" /src