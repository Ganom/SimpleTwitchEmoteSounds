;At start will be searched if the current system language is in this list,
;if not the first language in this list will be chosen as language
!insertmacro MUI_LANGUAGE "English"
!insertmacro MUI_LANGUAGE "German"

;Language strings

LangString LangStringCreateDesktopShortcut ${LANG_ENGLISH} "Create Desktop Shortcut"
LangString LangStringCreateDesktopShortcut ${LANG_GERMAN}  "Erstelle Desktop Verknüpfung"

LangString LangStrInstall ${LANG_ENGLISH} "Install"
LangString LangStrInstall ${LANG_GERMAN}  "Installiere"

LangString LangStrUninstall ${LANG_ENGLISH} "Uninstall"
LangString LangStrUninstall ${LANG_GERMAN}  "Deinstalliere"

LangString LangStrSettings ${LANG_ENGLISH} "Settings"
LangString LangStrSettings ${LANG_GERMAN}  "Einstellungen"

LangString LangStrRequired ${LANG_ENGLISH} "Required"
LangString LangStrRequired ${LANG_GERMAN}  "Notwendig"

LangString LangStrFailedToUninstallContinue ${LANG_ENGLISH} "Failed to uninstall, continue anyway?"
LangString LangStrFailedToUninstallContinue ${LANG_GERMAN}  "Deinstallation fehlgeschlagen, trotzdem fortfahren?"

LangString LangStrUninstallTheCurrentlyInstalled1 ${LANG_ENGLISH} "Uninstall the installed version "
LangString LangStrUninstallTheCurrentlyInstalled2 ${LANG_ENGLISH} " before installing "
LangString LangStrUninstallTheCurrentlyInstalled3 ${LANG_ENGLISH} "?"
LangString LangStrUninstallTheCurrentlyInstalled1 ${LANG_GERMAN}  "Möchten sie die installierte Version "
LangString LangStrUninstallTheCurrentlyInstalled2 ${LANG_GERMAN}  " vor der Installation von "
LangString LangStrUninstallTheCurrentlyInstalled3 ${LANG_GERMAN}  " deinstallieren?"

LangString LangStrRemoveSettings ${LANG_ENGLISH} "Do you want to remove all Settings?"
LangString LangStrRemoveSettings ${LANG_GERMAN}  "Sollen alle Einstellungen entfernt werden?"
