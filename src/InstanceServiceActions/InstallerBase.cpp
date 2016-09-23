#include "stdafx.h"
#include "InstallerBase.h"

CInstallerBase::CInstallerBase (MSIHANDLE hInstall)
{
	m_hInstall = hInstall;
	MSIHANDLE hDB = ::MsiGetActiveDatabase (hInstall);
	if (hDB != NULL)
	{
		m_dbActive.Attach (hDB);
	}
}

CInstallerBase::~CInstallerBase ()
{
	//m_dbActive.Free ();
}

CMsiDatabase& CInstallerBase::GetActiveDatabase ()
{
	return m_dbActive;
}

CString CInstallerBase::GetProperty (LPCTSTR pszPropertyName)
{
	DWORD dwLength = 0;
	CString strResult;
	UINT er = MsiGetProperty (m_hInstall, pszPropertyName, strResult.GetBuffer (1), &dwLength);
	while (er == ERROR_MORE_DATA)
	{
		dwLength += 1;
		er = MsiGetProperty (m_hInstall, pszPropertyName,
			strResult.GetBuffer (dwLength + 1), &dwLength);
	}
	if (er != ERROR_SUCCESS)
	{
		throw _com_error (HRESULT_FROM_WIN32(er));
	}
	strResult.ReleaseBuffer ((int) dwLength);
	return strResult;
}

CString CInstallerBase::GetFormattedProperty (LPCTSTR pszPropertyName)
{
	return GetFormattedString (GetProperty (pszPropertyName));
}

void CInstallerBase::SetProperty(LPCTSTR pszPropertyName, LPCTSTR pszValue)
{
	LogInfo (_T("SetProperty (%s='%s')"), pszPropertyName, pszValue);
	UINT er = MsiSetProperty (m_hInstall, pszPropertyName, pszValue);
	if (er != ERROR_SUCCESS)
	{
		LogError (_T("SetProperty failed. [%08lX]"), er);
		throw _com_error (HRESULT_FROM_WIN32(er));
	}
}

CString CInstallerBase::GetFormattedString (LPCTSTR pszString)
{
	CString strResult;
	if (_tcslen (pszString) > 0)
	{
		CMsiRecord record (1);
		record.SetString (0, pszString);

		DWORD dwLength = 0;
		UINT er = MsiFormatRecord (m_hInstall, record.GetHandle (), strResult.GetBuffer (1), &dwLength);
		while (er == ERROR_MORE_DATA)
		{
			++dwLength;
			er = MsiFormatRecord (m_hInstall, record.GetHandle (), strResult.GetBuffer (dwLength + 1), &dwLength);
		}
		HRESULT hr = HRESULT_FROM_WIN32(er);
		if (FAILED (hr))
		{
			throw _com_error (hr);
		}
		strResult.ReleaseBuffer (dwLength);
	}
	return strResult;
}

CString CInstallerBase::GetRecordFormattedString (CMsiRecord& record, int iField)
{
	if (record.IsNull (iField))
	{
		return _T("");
	}
	return GetFormattedString (record.GetString (iField));
}

CString CInstallerBase::ExtractNextTabDelimitedBlock (CString& strData)
{
	int index = strData.Find (_T('\t'));
	if (index != -1)
	{
		CString strResult = strData.Left (index);
		strData = strData.Mid (index + 1);
		return strResult;
	}
	else
	{
		CString strResult (strData);
		strData.Empty ();
		return strResult;
	}
}

CString CInstallerBase::ExtractNextBarDelimitedBlock (CString& strData)
{
	int index = strData.Find (_T('|'));
	if (index != -1)
	{
		CString strResult = strData.Left (index);
		strData = strData.Mid (index + 1);
		return strResult;
	}
	else
	{
		CString strResult (strData);
		strData.Empty ();
		return strResult;
	}
}

WCA_TODO CInstallerBase::GetTranslatedInstallMode (WCA_TODO todoScheduled)
{
	if (IsRollbackMode())
	{
        if (WCA_TODO_INSTALL == todoScheduled)
        {
            todoScheduled = WCA_TODO_UNINSTALL;
        }
        else if (WCA_TODO_UNINSTALL == todoScheduled)
        {
            todoScheduled = WCA_TODO_INSTALL;
        }
	}
	return todoScheduled;
}

bool CInstallerBase::IsRollbackMode ()
{
	bool fIsRollback = false;
	if (::MsiGetMode (m_hInstall, MSIRUNMODE_ROLLBACK))
	{
		fIsRollback = true;
	}
	return fIsRollback;
}

void CInstallerBase::GetComponentState (
	LPCTSTR pszComponent, INSTALLSTATE* pInstall, INSTALLSTATE* pAction)
{
	UINT er = MsiGetComponentState (m_hInstall, pszComponent, pInstall, pAction);
	HRESULT hr = HRESULT_FROM_WIN32(er);
	if (FAILED (hr))
	{
		throw _com_error (hr);
	}
}

void CInstallerBase::SetComponentState (
	LPCTSTR pszComponent, INSTALLSTATE isState)
{
	UINT er = MsiSetComponentState (m_hInstall, pszComponent, isState);
	if (er != ERROR_SUCCESS)
	{
		throw _com_error (HRESULT_FROM_WIN32(er));
	}
}

WCA_TODO CInstallerBase::GetComponentToDo(LPCTSTR pszComponentId)
{
	INSTALLSTATE isInstalled = INSTALLSTATE_UNKNOWN;
	INSTALLSTATE isAction = INSTALLSTATE_UNKNOWN;
	if (ERROR_SUCCESS != ::MsiGetComponentState (m_hInstall, 
		pszComponentId, &isInstalled, &isAction))
	{
		return WCA_TODO_UNKNOWN;
	}
    
	if (IsReInstalling(isInstalled, isAction))
	{
		return WCA_TODO_REINSTALL;
	}
	else if (IsUninstalling(isInstalled, isAction))
	{
		return WCA_TODO_UNINSTALL;
	}
	else if (IsInstalling(isInstalled, isAction))
	{
		return WCA_TODO_INSTALL;
	}
	else
	{
		return WCA_TODO_UNKNOWN;
	}
}

bool CInstallerBase::IsInstalling (INSTALLSTATE isInstalled, INSTALLSTATE isAction)
{
	return (INSTALLSTATE_LOCAL == isAction ||
			INSTALLSTATE_SOURCE == isAction ||
			(INSTALLSTATE_DEFAULT == isAction &&
			 (INSTALLSTATE_LOCAL == isInstalled ||
			  INSTALLSTATE_SOURCE == isInstalled)));
}

bool CInstallerBase::IsReInstalling(INSTALLSTATE isInstalled, INSTALLSTATE isAction)
{
	return ((INSTALLSTATE_LOCAL == isAction ||
			INSTALLSTATE_SOURCE == isAction ||
			INSTALLSTATE_DEFAULT == isAction) &&
			(INSTALLSTATE_LOCAL == isInstalled ||
			INSTALLSTATE_SOURCE == isInstalled));
}

bool CInstallerBase::IsUninstalling (INSTALLSTATE isInstalled, INSTALLSTATE isAction)
{
	return ((INSTALLSTATE_ABSENT == isAction ||
			 INSTALLSTATE_REMOVED == isAction) &&
			(INSTALLSTATE_LOCAL == isInstalled ||
			 INSTALLSTATE_SOURCE == isInstalled));
}

void CInstallerBase::DoDeferredAction (
	LPCTSTR pszCustomAction, LPCTSTR pszCustomActionData, UINT uCost)
{
	LogInfo (_T("DoDeferredAction [%s,%s,%d]"), 
		pszCustomAction, pszCustomActionData, uCost);
	if (pszCustomActionData != NULL && _tcslen (pszCustomActionData) > 0)
	{
		SetProperty (pszCustomAction, pszCustomActionData);
	}
	
	// Update progress bar
	ProgressMessage (uCost, true);

	// Run the action
	UINT er = MsiDoAction (m_hInstall, pszCustomAction);
	HRESULT hr = HRESULT_FROM_WIN32(er);
	if (FAILED (hr))
	{
		LogError (_T("DoAction failed. [%08lX]"), hr);
		throw _com_error (hr);
	}
}

void CInstallerBase::ProgressMessage (UINT uiCost, bool fExtendProgressBar)
{
	static bool fExplicitProgressMessages = false;
	try
	{
		HRESULT hr = S_OK;
		UINT er = ERROR_SUCCESS;
		CMsiRecord progress (3);

		// if aren't extending the progress bar and we haven't switched into explicit message mode
		if (!fExtendProgressBar && !fExplicitProgressMessages)
		{
			if (!(::MsiGetMode(m_hInstall, MSIRUNMODE_SCHEDULED) ||
				::MsiGetMode(m_hInstall, MSIRUNMODE_COMMIT) ||
				::MsiGetMode(m_hInstall, MSIRUNMODE_ROLLBACK)))
			{
				LogErrorMessage (_T("can only send progress bar messages in a deferred CustomAction"));
			}

			// tell Darwin to use explicit progress messages
			progress.SetInteger (1, 1);
			progress.SetInteger (2, 1);
			progress.SetInteger (3, 0);

			er = ProcessMessage (INSTALLMESSAGE_PROGRESS, progress);
			if (0 == er || IDOK == er || IDYES == er)
			{
				hr = S_OK;
			}
			else if (IDABORT == er || IDCANCEL == er)
			{
				throw _com_error (HRESULT_FROM_WIN32 (ERROR_INSTALL_USEREXIT));
			}
			else
			{
				hr = E_UNEXPECTED;
			}
			if (FAILED (hr))
			{
				LogError (_T("Failed to get Darwin into explicit mode. [%08lX]"), hr);
				throw _com_error (hr);
			}

			fExplicitProgressMessages = true;
		}
#if _DEBUG
		else if (fExtendProgressBar)   // if we are extending the progress bar, make sure we're not deferred
		{
			if (::MsiGetMode(m_hInstall, MSIRUNMODE_SCHEDULED))
			{
				LogErrorMessage (_T("cannot add ticks to progress bar length from deferred CustomAction"));
			}
		}
#endif

		// send the progress message
		progress.SetInteger (1, (fExtendProgressBar) ? 3 : 2);
		progress.SetInteger (2, uiCost);
		progress.SetInteger (3, 0);

		er = ProcessMessage (INSTALLMESSAGE_PROGRESS, progress);
		if (0 == er || IDOK == er || IDYES == er)
		{
			hr = S_OK;
		}
		else if (IDABORT == er || IDCANCEL == er)
		{
			throw _com_error (HRESULT_FROM_WIN32 (ERROR_INSTALL_USEREXIT));
		}
		else
		{
			throw _com_error (HRESULT_FROM_WIN32 (E_UNEXPECTED));
		}
	}
	catch (const _com_error& error)
	{
		LogError (_T("Caught exception in ProgressMessage : [%08lX]"),
			error.Error ());
	}
}

void CInstallerBase::LogMsiHandle (LPCTSTR pszType, MSIHANDLE handle)
{
	LogInfo (_T("Trace MSI handle [%d] as %s"), handle, pszType);
}

void CInstallerBase::LogInfo (LPCTSTR pszFormat, ...)
{
	CString strBuffer;
	va_list args;
	va_start (args, pszFormat);
	_vstprintf_s (strBuffer.GetBuffer (1024), 1024, pszFormat, args);
	strBuffer.ReleaseBuffer (-1);
	LogInfoMessage (strBuffer);
}

void CInstallerBase::LogInfoMessage (LPCTSTR pszText)
{
	LogMessage (INSTALLMESSAGE_INFO, pszText);
}

void CInstallerBase::LogUser (LPCTSTR pszFormat, ...)
{
	CString strBuffer;
	va_list args;
	va_start (args, pszFormat);
	_vstprintf_s (strBuffer.GetBuffer (1024), 1024, pszFormat, args);
	strBuffer.ReleaseBuffer (-1);
	LogUserMessage (strBuffer);
}

void CInstallerBase::LogUserMessage (LPCTSTR pszText)
{
	LogMessage (INSTALLMESSAGE_USER, pszText);
}

void CInstallerBase::LogWarning (LPCTSTR pszFormat, ...)
{
	CString strBuffer;
	va_list args;
	va_start (args, pszFormat);
	_vstprintf_s (strBuffer.GetBuffer (1024), 1024, pszFormat, args);
	strBuffer.ReleaseBuffer (-1);
	LogWarningMessage (strBuffer);
}

void CInstallerBase::LogWarningMessage (LPCTSTR pszText)
{
	LogMessage (INSTALLMESSAGE_WARNING, pszText);
}

void CInstallerBase::LogError (LPCTSTR pszFormat, ...)
{
	CString strBuffer;
	va_list args;
	va_start (args, pszFormat);
	_vstprintf_s (strBuffer.GetBuffer (1024), 1024, pszFormat, args);
	strBuffer.ReleaseBuffer (-1);
	LogErrorMessage (strBuffer);
}

void CInstallerBase::LogErrorMessage (LPCTSTR pszText)
{
	LogMessage (INSTALLMESSAGE_ERROR, pszText);
}

void CInstallerBase::LogMessage (INSTALLMESSAGE iMessageType, LPCTSTR pszText)
{
	try
	{
		CMsiRecord record (1);
		record.SetString (0, pszText);
		record.SetString (1, pszText);
		UINT er = ProcessMessage (iMessageType, record);
		if (er != ERROR_SUCCESS)
		{
			throw _com_error (HRESULT_FROM_WIN32(er));
		}
	}
#ifdef _DEBUG
	catch (const _com_error& error)
	{
		ATLTRACE (_T("Error caught in LogMessage : %08lX"), error.Error ());
	}
#else
	catch (...)
	{
	}
#endif
}

UINT CInstallerBase::ProcessMessage(INSTALLMESSAGE eMessageType, CMsiRecord& rRecord)
{
	try
	{
		UINT er = MsiProcessMessage (m_hInstall, eMessageType, 
			rRecord.GetHandle ());
		return er;
	}
	catch (const _com_error& error)
	{
#ifdef _DEBUG
		ATLTRACE (_T("Error caught in ProcessMessage : %08lX"), error.Error ());
#endif
		return error.WCode ();
	}
}
