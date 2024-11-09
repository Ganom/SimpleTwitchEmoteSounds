; Windows Installer

;--------------------------------
;Include Modern UI

  !include "MUI2.nsh"

;--------------------------------
;Include External Config File (that contains name, version, ...)

  !include "windows_installer_config.nsi"

;--------------------------------
;Include External Logic File To Run Previous Uninstaller If Detected

  !include "windows_installer_run_previous_uninstaller.nsi"

;--------------------------------
;General Settings

  ;Properly display all languages
  Unicode true

  ;Show 'console' in installer and uninstaller
  ShowInstDetails "show"
  ShowUninstDetails "show"

  ;Set name and output file
  Name "${PRODUCT}"
  OutFile "..\publish\${PRODUCT}_installer.exe"

  ;Set the default installation directory
  InstallDir "$LOCALAPPDATA\${PRODUCT}"

  ;Set separate installation directory for binary files (PATH bin directory)
  !define INSTDIR_BIN "bin"

  ;Set separate data directories
  !define DATADIR_SETTINGS "Settings"
  !define DATADIR_LOGS "Logs"

  ;Set registry keys
  !define REG_KEY_INSTALL_DIR "Software\${PRODUCT}"
  !define REG_KEY_UNINSTALL_INFO "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PRODUCT}"

  ;Set the estimated install size
  Var /GLOBAL ESTIMATED_INSTALL_SIZE_IN_KB

  ;Overwrite $InstallDir value when a previous installation directory was found
  InstallDirRegKey HKCU "${REG_KEY_INSTALL_DIR}" ""

  ;Set execution level to 'user' to avoid requiring admin
  ;This means that the install dir cannot be the program files directory!
  RequestExecutionLevel user

;--------------------------------
;Interface Settings

  ;Use a custom welcome page title
  !define MUI_WELCOMEPAGE_TITLE "${PRODUCT_DISPLAY_NAME} ${PRODUCT_VERSION}"

  ;Show warning if user wants to abort
  !define MUI_ABORTWARNING

  ;Show all languages, despite user's codepage
  !define MUI_LANGDLL_ALLLANGUAGES

  ;Use a custom (un-)install icon
  !define MUI_ICON "..\${PRODUCT}\Assets\cow.ico"
  ;!define MUI_UNICON "..\${PRODUCT}\Assets\cow.ico"

  ;Add a Desktop shortcut if the user wants to enable it on the finish page
  ;(https://stackoverflow.com/a/1517851)
  !define MUI_FINISHPAGE_SHOWREADME ""
  !define MUI_FINISHPAGE_SHOWREADME_NOTCHECKED
  !define MUI_FINISHPAGE_SHOWREADME_TEXT $(LangStringCreateDesktopShortcut)
  !define MUI_FINISHPAGE_SHOWREADME_FUNCTION createDesktopShortcut

;--------------------------------
;Pages

  ;For the installer:
  ;------------------------------
  ;Welcome page with name and version
  !insertmacro MUI_PAGE_WELCOME
  ;License page
  ;!insertmacro MUI_PAGE_LICENSE "..\LICENSE"
  ;Component selector
  ;!insertmacro MUI_PAGE_COMPONENTS
  ;Set install directory
  !insertmacro MUI_PAGE_DIRECTORY
  ;Show progress while installing/copying the files
  !insertmacro MUI_PAGE_INSTFILES
  ;Show final finish page
  !insertmacro MUI_PAGE_FINISH

  ;For the uninstaller:
  ;------------------------------
  ;Welcome page to uninstaller
  !insertmacro MUI_UNPAGE_WELCOME
  ;Confirm the uninstall with the install directory shown
  !insertmacro MUI_UNPAGE_CONFIRM
  ;Show progress while uninstalling/removing the files
  !insertmacro MUI_UNPAGE_INSTFILES
  ;Show final finish page
  !insertmacro MUI_UNPAGE_FINISH

;--------------------------------
;Include External Languages File

  !include "windows_installer_languages.nsi"

;--------------------------------
;Before Installer Section

Function .onInit
  ;If a previous installation was found ask the user via a popup if they want to
  ;uninstall it before running the installer
  ; Get uninstall information from HKCU instead of HKLM
  ReadRegStr $0 HKCU "${REG_KEY_UNINSTALL_INFO}" "UninstallString"
  ReadRegStr $1 HKCU "${REG_KEY_UNINSTALL_INFO}" "DisplayName"
  ReadRegStr $2 HKCU "${REG_KEY_UNINSTALL_INFO}" "DisplayVersion"
  ${If} $0 != ""
  ${AndIf} ${Cmd} `MessageBox MB_YESNO|MB_ICONQUESTION "$(LangStrUninstallTheCurrentlyInstalled1)$1 $2$(LangStrUninstallTheCurrentlyInstalled2)${PRODUCT_DISPLAY_NAME} ${PRODUCT_VERSION}$(LangStrUninstallTheCurrentlyInstalled3)" /SD IDYES IDYES`
    ;Use the included macro to uninstall the existing installation if the user
    ;selected yes
    !insertmacro UninstallExisting $0 $0
    ;If the uninstall failed show an additional popup window asking if the
    ;installation should be aborted or not
    ${If} $0 <> 0
      MessageBox MB_YESNO|MB_ICONSTOP "$(LangStrFailedToUninstallContinue)" /SD IDYES IDYES +2
        Abort
    ${EndIf}
  ${EndIf}

FunctionEnd

;--------------------------------
;Installer Section > Main Component

Section "${PRODUCT_DISPLAY_NAME} ($(LangStrRequired))" Section1

  DetailPrint "$(LangStrInstall) ${PRODUCT_DISPLAY_NAME} ${PRODUCT_VERSION}"

  ;This will prevent this component from being disabled on the selection page
  SectionIn RO

  ;Set output path to the installation directory and list the files that should
  ;be put into it
  SetOutPath "$INSTDIR"
  ;Icon for shortcuts
  File "/oname=${PRODUCT}.ico" "..\${PRODUCT}\Assets\cow.ico"
  ;Separate installation directory for binary files (PATH bin directory)
  SetOutPath "$INSTDIR\${INSTDIR_BIN}"
  ;Binary executable of program
  File "..\publish\${PRODUCT}.exe"
  StrCpy $ESTIMATED_INSTALL_SIZE_IN_KB / 32011

  ;Store installation folder in registry for future installs under HKCU
  WriteRegStr   HKCU "${REG_KEY_INSTALL_DIR}" "" "$INSTDIR"

  ;Register the application in Add/Remove Programs under HKCU
  WriteRegStr   HKCU "${REG_KEY_UNINSTALL_INFO}" "DisplayName"          "${PRODUCT_DISPLAY_NAME}"
  WriteRegStr   HKCU "${REG_KEY_UNINSTALL_INFO}" "DisplayVersion"       "${PRODUCT_VERSION}"
  WriteRegStr   HKCU "${REG_KEY_UNINSTALL_INFO}" "DisplayIcon"          "$INSTDIR\${PRODUCT}.ico"
  WriteRegStr   HKCU "${REG_KEY_UNINSTALL_INFO}" "Publisher"            "${PRODUCT_PUBLISHER}"
  WriteRegStr   HKCU "${REG_KEY_UNINSTALL_INFO}" "URLInfoAbout"         "${PRODUCT_URL}"
  WriteRegStr   HKCU "${REG_KEY_UNINSTALL_INFO}" "UninstallString"      "$\"$INSTDIR\${PRODUCT}_uninstaller.exe$\""
  WriteRegStr   HKCU "${REG_KEY_UNINSTALL_INFO}" "QuietUninstallString" "$\"$INSTDIR\${PRODUCT}_uninstaller.exe$\" /S"
  WriteRegDWORD HKCU "${REG_KEY_UNINSTALL_INFO}" "NoModify"             1
  WriteRegDWORD HKCU "${REG_KEY_UNINSTALL_INFO}" "NoRepair"             1
  WriteRegDWORD HKCU "${REG_KEY_UNINSTALL_INFO}" "EstimatedSize"        $ESTIMATED_INSTALL_SIZE_IN_KB

  ;Create default settings directory
  CreateDirectory "$APPDATA\${PRODUCT}"
  CreateDirectory "$APPDATA\${PRODUCT}\${DATADIR_SETTINGS}"

  ;Create start menu shortcut for program, settings directory, and uninstaller
  CreateDirectory "$SMPROGRAMS\${PRODUCT}"
  CreateShortCut  "$SMPROGRAMS\${PRODUCT}\${PRODUCT_DISPLAY_NAME}.lnk"                     "$INSTDIR\${INSTDIR_BIN}\${PRODUCT}.exe" "" "$INSTDIR\${PRODUCT}.ico" 0
  CreateShortCut  "$SMPROGRAMS\${PRODUCT}\${PRODUCT_DISPLAY_NAME} $(LangStrSettings).lnk"  "$APPDATA\${PRODUCT}\${DATADIR_SETTINGS}"
  CreateShortCut  "$SMPROGRAMS\${PRODUCT}\$(LangStrUninstall) ${PRODUCT_DISPLAY_NAME}.lnk" "$INSTDIR\${PRODUCT}_uninstaller.exe" "" "$INSTDIR\${PRODUCT}_uninstaller.exe" 0

  ;Create uninstaller
  WriteUninstaller "$INSTDIR\${PRODUCT}_uninstaller.exe"

SectionEnd

;--------------------------------
;Uninstaller Section

Section "Uninstall"

  DetailPrint "$(LangStrUninstall) ${PRODUCT_DISPLAY_NAME} ${PRODUCT_VERSION}"

  ;Remove registry keys
  DeleteRegKey HKCU "${REG_KEY_UNINSTALL_INFO}"
  DeleteRegKey HKCU "${REG_KEY_INSTALL_DIR}"

  ;Remove the installation directory and all files within it
  RMDir /r "$INSTDIR\${INSTDIR_BIN}"
  RMDir /r "$INSTDIR"

  ;Remove the start menu directory and all shortcuts within it
  Delete "$SMPROGRAMS\${PRODUCT}\*.*"
  RmDir  "$SMPROGRAMS\${PRODUCT}"

  ${If} ${Silent}
    ;In silent mode do not ask for removal of settings directory
    ;Reasoning: Upgrading takes one less click
  ${Else}
    ;Confirm whether to delete the settings directory
    MessageBox MB_YESNO|MB_ICONQUESTION "$(LangStrRemoveSettings)" IDYES +2
    Goto skip_delete_settings_directory

    ;Remove the settings directory and all files within it
    RMDir /r "$APPDATA\${PRODUCT}\${DATADIR_SETTINGS}"
    RMDir /r "$APPDATA\${PRODUCT}\${DATADIR_LOGS}"
    RMDir /r "$APPDATA\${PRODUCT}"

    skip_delete_settings_directory:
  ${EndIf}

SectionEnd

;--------------------------------
;After Installation Function (is triggered after a successful installation)

Function .onInstSuccess

  ;Run the product after a successful installation
  Exec "$INSTDIR\${INSTDIR_BIN}\${PRODUCT}.exe"

FunctionEnd

;--------------------------------
;Custom Function To Create A Desktop Shortcut

Function createDesktopShortcut

  ;Create Desktop shortcut to main component
  CreateShortCut "$DESKTOP\${PRODUCT_DISPLAY_NAME}.lnk" "$INSTDIR\${INSTDIR_BIN}\${PRODUCT}.exe" "" "$INSTDIR\${PRODUCT}.ico" 0

FunctionEnd
