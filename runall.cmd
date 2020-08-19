:: full workflow

cd DocRepoParser
dotnet run %1 %2

if %errorlevel% neq 0 exit /b %errorlevel%

cd ..
cd SparkWordsProcessor
dotnet build
cd bin/Debug/netcoreapp3.1
call runjob.cmd %1

cd ../../../../DocMLCategorization

dotnet run %1