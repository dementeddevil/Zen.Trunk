#pragma once

class CMsiHandle
{
public:
	CMsiHandle ()
	{
		m_hHandle = NULL;
	}

	CMsiHandle (MSIHANDLE hHandle)
	{
		m_hHandle = hHandle;
	}

	CMsiHandle (CMsiHandle& original)
	{
		m_hHandle = original.Detach ();
	}

	virtual ~CMsiHandle ()
	{
		Free ();
	}

	CMsiHandle& operator= (CMsiHandle& rhs)
	{
		if (m_hHandle == rhs.m_hHandle)
		{
			ATLASSERT (FALSE);
		}
		else
		{
			Free ();
			Attach (rhs.Detach ());
		}
		return *this;
	}
    bool operator!=(CMsiHandle& p) const
    {
        return !operator==(p);
    }
    bool operator==(CMsiHandle& p) const
    {
		return m_hHandle==p.m_hHandle;
    }
	MSIHANDLE operator* () const
	{
		return m_hHandle;
	}
	MSIHANDLE* operator& ()
	{
		return &m_hHandle;
	}

	MSIHANDLE GetHandle () const
	{
		return m_hHandle;
	}
	MSIHANDLE* GetHandlePtr ()
	{
		return &m_hHandle;
	}

	void Attach (MSIHANDLE handle)
	{
		Free ();
		m_hHandle = handle;
	}

	MSIHANDLE Detach ()
	{
		MSIHANDLE handle = m_hHandle;
		m_hHandle = NULL;
		return handle;
	}

	virtual void Free ()
	{
		if (m_hHandle != NULL)
		{
			MsiCloseHandle (m_hHandle);
			m_hHandle = NULL;
		}
	}

private:
	MSIHANDLE m_hHandle;
};

class CMsiRecord : public CMsiHandle
{
public:
	CMsiRecord ()
	{
	}

	CMsiRecord (int iFieldCount)
		: CMsiHandle (MsiCreateRecord (iFieldCount))
	{
	}

	CMsiRecord (MSIHANDLE record)
		: CMsiHandle (record)
	{
	}

	CMsiRecord (CMsiRecord& original)
		: CMsiHandle (original)
	{
	}

	void ClearData ()
	{
		MsiRecordClearData (GetHandle ());
	}

	UINT GetDataSize (int iField)
	{
		return MsiRecordDataSize (GetHandle (), iField);
	}

	int GetInteger (int iField)
	{
		return MsiRecordGetInteger (GetHandle (), iField);
	}

	bool IsNull (int iField)
	{
		return MsiRecordIsNull (GetHandle (), iField) != 0 ? true : false;
	}

	CString GetString (int iField)
	{
		CString strBuffer;
		if (!IsNull (iField))
		{
			USES_CONVERSION;
			DWORD dwLength = 0;
			UINT result = MsiRecordGetString (GetHandle (), iField, strBuffer.GetBuffer (1), &dwLength);
			while (result != ERROR_SUCCESS)
			{
				if (result != ERROR_MORE_DATA)
				{
					throw _com_error (HRESULT_FROM_WIN32(result));
				}
				dwLength += 1;
				result = MsiRecordGetString (GetHandle (), iField, 
					strBuffer.GetBuffer (dwLength + 1), &dwLength);
			}
			HRESULT hr = HRESULT_FROM_WIN32(result);
			if (FAILED (hr))
			{
				throw _com_error (hr);
			}
			strBuffer.ReleaseBuffer (dwLength);
		}
		return strBuffer;
	}

	void SetInteger (int iField, int value)
	{
		MsiRecordSetInteger (GetHandle (), iField, value);
	}

	void SetString (int iField, LPCTSTR pszValue)
	{
		MsiRecordSetString (GetHandle (), iField, pszValue);
	}

private:
};

class CMsiView : public CMsiHandle
{
public:
	CMsiView ()
	{
	}

	CMsiView (MSIHANDLE view)
		: CMsiHandle (view)
	{
	}

	CMsiView (CMsiView& original)
		: CMsiHandle (original)
	{
	}

	void Close ()
	{
		if (m_bNeedsClose)
		{
			MsiViewClose (GetHandle ());
			m_bNeedsClose = false;
		}
	}

	void Execute ()
	{
		UINT er = MsiViewExecute (GetHandle(), NULL);
		HRESULT hr = HRESULT_FROM_WIN32(er);
		if (FAILED(hr))
		{
			throw _com_error (hr);
		}
	}

	void Execute (CMsiRecord& rRecord)
	{
		MSIHANDLE hRecord = rRecord.GetHandle ();
		UINT er = MsiViewExecute (GetHandle(), hRecord);
		HRESULT hr = HRESULT_FROM_WIN32(er);
		if (FAILED(hr))
		{
			throw _com_error (hr);
		}
	}

	bool Fetch (CMsiRecord& rRecord)
	{
		// Free the record first...
		rRecord.Free ();

		// Then attempt fetch
		bool bMore = false;
		UINT er = MsiViewFetch (GetHandle (), rRecord.GetHandlePtr ());
		if (er == ERROR_SUCCESS)
		{
			m_bNeedsClose = true;
			return true;
		}
		if (er != ERROR_NO_MORE_ITEMS)
		{
			throw _com_error (HRESULT_FROM_WIN32(er));
		}
		m_bNeedsClose = false;
		return false;
	}

	bool FetchSingleRecord (CMsiRecord& rRecord)
	{
		bool bMore = Fetch (rRecord);
		Close ();
		return bMore;
	}

protected:
	virtual void Free ()
	{
		Close ();
		CMsiHandle::Free ();
	}

private:
	bool m_bNeedsClose;
};

class CMsiDatabase : public CMsiHandle
{
public:
	CMsiDatabase ()
	{
	}

	CMsiDatabase (MSIHANDLE database)
		: CMsiHandle (database)
	{
	}

	CMsiDatabase (CMsiDatabase& original)
		: CMsiHandle (original)
	{
	}

	bool IsValid () const
	{
		return (GetHandle() != NULL) ? true : false;
	}

	bool IsTable (LPCTSTR pszTableName)
	{
		CMsiHandle record;
		UINT er = ::MsiDatabaseGetPrimaryKeys(GetHandle(), pszTableName, record.GetHandlePtr ());
		if (ERROR_SUCCESS == er)
			return true;
		else if (ERROR_INVALID_TABLE == er)
			return false;
		throw _com_error (HRESULT_FROM_WIN32(er));
	}

	CMsiView OpenView (LPCTSTR pszSql)
	{
		CMsiView tView;
		UINT er = MsiDatabaseOpenView (GetHandle (), pszSql, tView.GetHandlePtr ());
		HRESULT hr = HRESULT_FROM_WIN32 (er);
		if (FAILED (hr))
		{
			throw _com_error (hr);
		}
		return tView;
	}

	CMsiView OpenExecuteView (LPCTSTR pszSql)
	{
		CMsiView tView;
		UINT er = MsiDatabaseOpenView (GetHandle (), pszSql, tView.GetHandlePtr ());
		HRESULT hr = HRESULT_FROM_WIN32 (er);
		if (FAILED (hr))
		{
			throw _com_error (hr);
		}
		tView.Execute ();
		return tView;
	}

	CMsiView OpenExecuteView (LPCTSTR pszSql, CMsiRecord& record)
	{
		CMsiView tView;
		UINT er = MsiDatabaseOpenView (GetHandle (), pszSql, tView.GetHandlePtr ());
		HRESULT hr = HRESULT_FROM_WIN32 (er);
		if (FAILED (hr))
		{
			throw _com_error (hr);
		}
		tView.Execute (record);
		return tView;
	}
};

