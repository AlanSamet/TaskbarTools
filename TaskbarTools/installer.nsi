; Define some installation variables
!define AppName "TaskBarTools"
Name ${AppName}
!define CompanyName "AlanSamet"
!define Version "1.0.0"
!define InstallDir "$PROGRAMFILES\${AppName}"

; Include necessary plugins and libraries
!include "MUI2.nsh"
!include "FileFunc.nsh"
!include "LogicLib.nsh"

; Define the name of the installer
OutFile "Setup${AppName}.exe"

; Define the default installation directory
InstallDir ${InstallDir}

; Pages
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

!insertmacro MUI_LANGUAGE "English"

Section "MainSection" SEC01
  SetOutPath $INSTDIR
  File /r "bin\debug\net7.0-windows\*.*"
  CreateDirectory "$SMPROGRAMS\${AppName}"
  CreateShortCut "$SMPROGRAMS\${AppName}\${AppName}.lnk" "$INSTDIR\${AppName}.exe"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Run" ${AppName} "$INSTDIR\${AppName}.exe"

  ; Create uninstaller
  WriteUninstaller "$INSTDIR\Uninstall${AppName}.exe"
SectionEnd

Section "Uninstall"
  Delete "$SMPROGRAMS\${AppName}\${AppName}.lnk"
  Delete "$INSTDIR\*.*"
  DeleteRegValue HKCU "Software\Microsoft\Windows\CurrentVersion\Run" ${AppName}
  RMDir /r "$INSTDIR"
  RMDir "$SMPROGRAMS\${AppName}"
  
  ; Remove the uninstaller itself
  Delete "$INSTDIR\Uninstall${AppName}.exe"
SectionEnd

Function .onInstSuccess
  ; Start the application when the installation is successful
  Exec "$INSTDIR\${AppName}.exe"
FunctionEnd