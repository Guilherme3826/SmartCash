@echo off
set "targetDir=C:\Users\guilh\OneDrive\Documentos\SmartCash"

echo ---------------------------------------------------------
echo Iniciando limpeza bruta de bin/obj em: 
echo %targetDir%
echo ---------------------------------------------------------

cd /d "%targetDir%"

:: Procura e remove as pastas 'obj' de forma recursiva
for /d /r . %%d in (obj) do (
    if exist "%%d" (
        echo Removendo: %%d
        rd /s /q "%%d"
    )
)

:: Procura e remove as pastas 'bin' de forma recursiva
for /d /r . %%d in (bin) do (
    if exist "%%d" (
        echo Removendo: %%d
        rd /s /q "%%d"
    )
)

echo.
echo ---------------------------------------------------------
echo Limpeza concluida! Agora voce pode reabrir o Visual Studio.
echo ---------------------------------------------------------
pause