@echo off
echo ========================================================
echo Building STX Enterprises Solution
echo ========================================================
dotnet build src\STXEnterprises.sln -c Release
if %ERRORLEVEL% NEQ 0 (
    echo Build failed!
    exit /b %ERRORLEVEL%
)

echo.
echo ========================================================
echo Publishing Single-File Portable Distribution to dist\
echo ========================================================
dotnet publish src\STXEnterprises\STXEnterprises.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o dist
if %ERRORLEVEL% NEQ 0 (
    echo Publish failed!
    exit /b %ERRORLEVEL%
)

echo.
echo Copying runtime assets to dist...
xcopy /E /I /Y "wwwroot" "dist\wwwroot"
xcopy /E /I /Y "bin" "dist\bin"
copy /Y "README.txt" "dist\"
if exist "dist\STXEnterprises.exe" copy /Y "dist\STXEnterprises.exe" "STXEnterprises.exe" >nul 2>&1

echo.
echo ========================================================
echo Build and packaging complete! Output is in dist\
echo ========================================================
