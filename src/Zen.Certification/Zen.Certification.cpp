// This is the main DLL file.

#include "stdafx.h"

#include <vector>

#include "Zen.Certification.h"

using namespace System::Collections;
using namespace System::IO;
using namespace System::Security::Cryptography::X509Certificates;
using namespace Zen::Certification;

Certificate::Certificate(String^ storePathName)
{
	_storePathName = storePathName;
}

/// <summary>
/// Creates the certificate with an exportable private key protected with the
///	specified password.
/// </summary>
/// <param name="x500">The X500.</param>
/// <param name="startTime">The start time.</param>
/// <param name="endTime">The end time.</param>
/// <param name="password">The password.</param>
/// <param name="keyLength">Length of the key.</param>
/// <returns></returns>
///	<remarks>
///	The generated certificate has a critical key usage extension added so it
///	may only be used for client authentication purposes.
///	The certificate uses RSA public/private keys and SHA512 hashing algorithm
///	It is recommended that the key-length be set to at least 1024bits.
///	</remarks>
array<Byte>^ Certificate::CreateSelfSignedCertificate(
	String^ x500,
	DateTime startTime,
	DateTime endTime,
	SecureString^ password,
	int keyLength)
{
	SYSTEMTIME startSystemTime = ToSystemTime(startTime);
	SYSTEMTIME endSystemTime = ToSystemTime(endTime);
	String^ containerName = Guid::NewGuid().ToString();

	pin_ptr<const wchar_t> wszContainerName = PtrToStringChars(containerName);
	HCRYPTPROV hProvider = NULL;
	HCRYPTKEY hKey = NULL;
	PCCERT_CONTEXT hCertContext = NULL;
	PCCERT_CONTEXT hStoreCertContext = NULL;
	HCERTSTORE hCertStore = NULL;
	IntPtr passwordPtr;
	CERT_NAME_BLOB subjectNameBlob;
	CERT_EXTENSION clientAuthenticationExtension;
	try
	{
		// Acquire crypto context
		Check(CryptAcquireContext(
			&hProvider,
			wszContainerName,
			NULL,
			PROV_RSA_FULL,
			CRYPT_NEWKEYSET));

		// Generate key of appropriate length
		Check(CryptGenKey(
			hProvider,
			AT_KEYEXCHANGE,
			CRYPT_EXPORTABLE | keyLength << 16,
			&hKey));

		// Convert X500 name into byte array
		subjectNameBlob = ToCertName(x500);

		// Prepare crypto key provider info (not needed but belt and braces)
		CRYPT_KEY_PROV_INFO kpi;
		kpi.pwszContainerName = const_cast<LPWSTR>(wszContainerName);
		kpi.dwProvType = PROV_RSA_FULL;
		kpi.dwKeySpec = AT_KEYEXCHANGE;

		// Determine algorithm to use
		// TODO: Allow this to arrive via parameter
		CRYPT_ALGORITHM_IDENTIFIER algid;
		algid.pszObjId = szOID_RSA_SHA512RSA;

		// Determine usage policy
		CERT_POLICY_ID clientAuthPolicy;
		clientAuthPolicy.cCertPolicyElementId = 1;
		clientAuthPolicy.rgpszCertPolicyElementId =
			(LPSTR*)&szOID_PKIX_KP_CLIENT_AUTH;
		CERT_KEY_USAGE_RESTRICTION_INFO ckuri;
		ckuri.cCertPolicyId = 1;
		ckuri.rgCertPolicyId = &clientAuthPolicy;

		// Encode the usage policy information
		clientAuthenticationExtension = ToCertExtension(
			szOID_KEY_USAGE_RESTRICTION, &ckuri, TRUE);

		CERT_EXTENSIONS extensions;
		extensions.cExtension = 1;
		extensions.rgExtension = &clientAuthenticationExtension;

		hCertContext = CertCreateSelfSignCertificate(
			hProvider,
			&subjectNameBlob,
			0,
			&kpi,
			&algid,
			&startSystemTime,
			&endSystemTime,
			&extensions);
		Check(hCertContext != NULL);

		hCertStore = CertOpenStore(
			"Memory", 0, NULL, CERT_STORE_CREATE_NEW_FLAG, NULL);
		Check(hCertStore != NULL);

		Check(CertAddCertificateContextToStore(
			hCertStore,
			hCertContext,
			CERT_STORE_ADD_NEW,
			&hStoreCertContext));

		CertSetCertificateContextProperty(
			hStoreCertContext,
			CERT_KEY_PROV_INFO_PROP_ID,
			0,
			&kpi);

		if (password != nullptr)
		{
			passwordPtr = System::Runtime::InteropServices::Marshal::SecureStringToCoTaskMemUnicode(password);
		}

		CRYPT_DATA_BLOB pfxBlob;
		Check(PFXExportCertStoreEx(
			hCertStore,
			&pfxBlob,
			(LPCWSTR)passwordPtr.ToPointer(),
			NULL,
			EXPORT_PRIVATE_KEYS | REPORT_NO_PRIVATE_KEY | REPORT_NOT_ABLE_TO_EXPORT_PRIVATE_KEY));

		array<Byte>^ pfxData = gcnew array<Byte>(pfxBlob.cbData);
		pin_ptr<Byte> ppfxData = &pfxData[0];
		pfxBlob.pbData = ppfxData;
		Check(PFXExportCertStoreEx(
			hCertStore,
			&pfxBlob,
			(LPCWSTR)passwordPtr.ToPointer(),
			NULL,
			EXPORT_PRIVATE_KEYS | REPORT_NO_PRIVATE_KEY | REPORT_NOT_ABLE_TO_EXPORT_PRIVATE_KEY));

		return pfxData;
	}
	finally
	{
		FreeCertNameBlob(subjectNameBlob);
		FreeCertExtension(clientAuthenticationExtension);
		if(passwordPtr != IntPtr::Zero)
		{
			System::Runtime::InteropServices::Marshal::ZeroFreeCoTaskMemUnicode(passwordPtr);
		}
		if(hCertContext != NULL)
		{
			CertFreeCertificateContext(hCertContext);
		}
		if(hStoreCertContext != NULL)
		{
			CertFreeCertificateContext(hStoreCertContext);
		}
		if(hCertStore != NULL)
		{
			CertCloseStore(hCertStore, 0);
		}
		if(hKey != NULL)
		{
			CryptDestroyKey(hKey);
		}
		if(hProvider != NULL)
		{
			CryptReleaseContext(hProvider, 0);
			CryptAcquireContext(
				&hProvider,
				wszContainerName,
				NULL,
				PROV_RSA_FULL,
				CRYPT_DELETEKEYSET);
		}
	}
}

/// <summary>
/// Creates the self signed CA certificate.
/// </summary>
/// <param name="x500">The X500.</param>
/// <param name="startTime">The start time.</param>
/// <param name="endTime">The end time.</param>
/// <param name="password">The password.</param>
/// <param name="keyLength">Length of the key.</param>
/// <param name="limitPathLength">Length of the limit path.</param>
/// <param name="pathLengthConstraint">The path length constraint.</param>
/// <returns></returns>
array<Byte>^ Certificate::CreateSelfSignedCACertificate(
	String^ x500,
	DateTime startTime,
	DateTime endTime,
	SecureString^ password,
	int keyLength,
	bool limitPathLength,
	int pathLengthConstraint,
	Uri^ crlDistributionUri)
{
	SYSTEMTIME startSystemTime = ToSystemTime(startTime);
	SYSTEMTIME endSystemTime = ToSystemTime(endTime);
	String^ containerName = Guid::NewGuid().ToString();

	pin_ptr<const wchar_t> wszContainerName = PtrToStringChars(containerName);
	HCRYPTPROV hProvider = NULL;
	HCRYPTKEY hKey = NULL;
	PCCERT_CONTEXT hCertContext = NULL;
	PCCERT_CONTEXT hStoreCertContext = NULL;
	HCERTSTORE hCertStore = NULL;
	IntPtr passwordPtr;
	CERT_NAME_BLOB subjectNameBlob;
	CERT_EXTENSION clientAuthenticationExtension;
	CERT_EXTENSION basicConstraintExtension;
	CERT_EXTENSION crlDistributionPointExtension;
	CERT_EXTENSION authorityInfoAccessExtension;
	try
	{
		// Acquire crypto context
		Check(CryptAcquireContext(
			&hProvider,
			wszContainerName,
			NULL,
			PROV_RSA_FULL,
			CRYPT_NEWKEYSET));

		// Generate key of appropriate length
		Check(CryptGenKey(
			hProvider,
			AT_KEYEXCHANGE,
			CRYPT_EXPORTABLE | keyLength << 16,
			&hKey));

		// Convert X500 name into byte array
		subjectNameBlob = ToCertName(x500);

		// Prepare crypto key provider info (not needed but belt and braces)
		CRYPT_KEY_PROV_INFO kpi;
		kpi.pwszContainerName = const_cast<LPWSTR>(wszContainerName);
		kpi.dwProvType = PROV_RSA_FULL;
		kpi.dwKeySpec = AT_KEYEXCHANGE;

		// Determine algorithm to use
		// TODO: Allow this to arrive via parameter
		CRYPT_ALGORITHM_IDENTIFIER algid;
		algid.pszObjId = szOID_RSA_SHA512RSA;

		// Determine usage policy
		CERT_POLICY_ID clientAuthPolicy;
		clientAuthPolicy.cCertPolicyElementId = 1;
		clientAuthPolicy.rgpszCertPolicyElementId =
			(LPSTR*)&szOID_PKIX_KP_CLIENT_AUTH;
		CERT_KEY_USAGE_RESTRICTION_INFO ckuri;
		ckuri.cCertPolicyId = 1;
		ckuri.rgCertPolicyId = &clientAuthPolicy;

		// Encode the usage policy information
		clientAuthenticationExtension = ToCertExtension(
			szOID_KEY_USAGE_RESTRICTION, &ckuri, TRUE);

		// Setup basic constraints
		CERT_BASIC_CONSTRAINTS2_INFO cbci;
		cbci.fCA = TRUE;
		if(!limitPathLength)
		{
			cbci.fPathLenConstraint = FALSE;
		}
		else
		{
			cbci.fPathLenConstraint = TRUE;
			cbci.dwPathLenConstraint = pathLengthConstraint;
		}
		basicConstraintExtension = ToCertExtension(
			szOID_BASIC_CONSTRAINTS2, &cbci, TRUE);

		// Build CRL distribution points
		CERT_ALT_NAME_ENTRY crlNameEntry;
		crlNameEntry.dwAltNameChoice = CERT_ALT_NAME_URL;
		String^ urlString = crlDistributionUri->ToString();
		pin_ptr<const wchar_t> wszCrlDistributionUri = PtrToStringChars(urlString);
		crlNameEntry.pwszURL = const_cast<LPWSTR>(wszCrlDistributionUri);
		CRL_DIST_POINT cdp;
		cdp.DistPointName.dwDistPointNameChoice = CRL_DIST_POINT_FULL_NAME;
		cdp.DistPointName.FullName.cAltEntry = 1;
		cdp.DistPointName.FullName.rgAltEntry = &crlNameEntry; 
		CRL_DIST_POINTS_INFO cdpi;
		cdpi.cDistPoint = 1;
		cdpi.rgDistPoint = &cdp;
		crlDistributionPointExtension = ToCertExtension(
			szOID_CRL_DIST_POINTS, &cdpi, FALSE);

		PCERT_EXTENSION extensionArray = new CERT_EXTENSION[3];
		extensionArray[0] = clientAuthenticationExtension;
		extensionArray[1] = basicConstraintExtension;
		extensionArray[2] = crlDistributionPointExtension;

		CERT_EXTENSIONS extensions;
		extensions.cExtension = 3;
		extensions.rgExtension = extensionArray;

		hCertContext = CertCreateSelfSignCertificate(
			hProvider,
			&subjectNameBlob,
			0,
			&kpi,
			&algid,
			&startSystemTime,
			&endSystemTime,
			&extensions);
		Check(hCertContext != NULL);

		hCertStore = CertOpenStore(
			"Memory", 0, NULL, CERT_STORE_CREATE_NEW_FLAG, NULL);
		Check(hCertStore != NULL);

		Check(CertAddCertificateContextToStore(
			hCertStore,
			hCertContext,
			CERT_STORE_ADD_NEW,
			&hStoreCertContext));

		CertSetCertificateContextProperty(
			hStoreCertContext,
			CERT_KEY_PROV_INFO_PROP_ID,
			0,
			&kpi);

		if (password != nullptr)
		{
			passwordPtr = System::Runtime::InteropServices::Marshal::SecureStringToCoTaskMemUnicode(password);
		}

		CRYPT_DATA_BLOB pfxBlob;
		Check(PFXExportCertStoreEx(
			hCertStore,
			&pfxBlob,
			(LPCWSTR)passwordPtr.ToPointer(),
			NULL,
			EXPORT_PRIVATE_KEYS | REPORT_NO_PRIVATE_KEY | REPORT_NOT_ABLE_TO_EXPORT_PRIVATE_KEY));

		array<Byte>^ pfxData = gcnew array<Byte>(pfxBlob.cbData);
		pin_ptr<Byte> ppfxData = &pfxData[0];
		pfxBlob.pbData = ppfxData;
		Check(PFXExportCertStoreEx(
			hCertStore,
			&pfxBlob,
			(LPCWSTR)passwordPtr.ToPointer(),
			NULL,
			EXPORT_PRIVATE_KEYS | REPORT_NO_PRIVATE_KEY | REPORT_NOT_ABLE_TO_EXPORT_PRIVATE_KEY));

		return pfxData;
	}
	finally
	{
		FreeCertNameBlob(subjectNameBlob);
		FreeCertExtension(clientAuthenticationExtension);
		FreeCertExtension(basicConstraintExtension);
		FreeCertExtension(crlDistributionPointExtension);
		if(passwordPtr != IntPtr::Zero)
		{
			System::Runtime::InteropServices::Marshal::ZeroFreeCoTaskMemUnicode(passwordPtr);
		}
		if(hCertContext != NULL)
		{
			CertFreeCertificateContext(hCertContext);
		}
		if(hStoreCertContext != NULL)
		{
			CertFreeCertificateContext(hStoreCertContext);
		}
		if(hCertStore != NULL)
		{
			CertCloseStore(hCertStore, 0);
		}
		if(hKey != NULL)
		{
			CryptDestroyKey(hKey);
		}
		if(hProvider != NULL)
		{
			CryptReleaseContext(hProvider, 0);
			CryptAcquireContext(
				&hProvider,
				wszContainerName,
				NULL,
				PROV_RSA_FULL,
				CRYPT_DELETEKEYSET);
		}
	}
}

array<Byte>^ Certificate::CreateSignedCertificate(
	X509Certificate2^ signingCert,
	Guid serialNumber,
	String^ x500,
	DateTime startTime,
	DateTime endTime,
	SecureString^ password,
	int keyLength)
{
	FILETIME startSystemTime = ToFileTime(startTime);
	FILETIME endSystemTime = ToFileTime(endTime);
	String^ containerName = Guid::NewGuid().ToString();

	pin_ptr<const wchar_t> wszContainerName = PtrToStringChars(containerName);
	HCRYPTPROV hProvider = NULL;
	HCRYPTKEY hKey = NULL;
	PCCERT_CONTEXT hCertContext = NULL;
	PCCERT_CONTEXT hStoreCertContext = NULL;
	HCERTSTORE hCertStore = NULL;
	IntPtr passwordPtr;
	CRYPT_INTEGER_BLOB serialNumberBlob;
	CERT_NAME_BLOB subjectNameBlob;
	CERT_NAME_BLOB issuerNameBlob;
	CRYPT_BIT_BLOB issuerUniqueIdBlob;
	CRYPT_BIT_BLOB subjectUniqueIdBlob;
	CERT_EXTENSION clientAuthenticationExtension;
	try
	{
		// Acquire crypto context
		Check(CryptAcquireContext(
			&hProvider,
			wszContainerName,
			NULL,
			PROV_RSA_FULL,
			CRYPT_NEWKEYSET));

		// Generate key of appropriate length
		Check(CryptGenKey(
			hProvider,
			AT_KEYEXCHANGE,
			CRYPT_EXPORTABLE | keyLength << 16,
			&hKey));

		// Convert X500 name into byte array
		subjectNameBlob = ToCertName(x500);

		// Convert signing cert issuer name into byte array
		issuerNameBlob = ToCertName(signingCert->Issuer);

		// Convert serial number
		serialNumberBlob = ToSerialNumberBlob(serialNumber);

		// Prepare crypto key provider info (not needed but belt and braces)
		CRYPT_KEY_PROV_INFO kpi;
		kpi.pwszContainerName = const_cast<LPWSTR>(wszContainerName);
		kpi.dwProvType = PROV_RSA_FULL;
		kpi.dwKeySpec = AT_KEYEXCHANGE;

		// Determine algorithm to use
		// TODO: Allow this to arrive via parameter
		CRYPT_ALGORITHM_IDENTIFIER algid;
		algid.pszObjId = szOID_RSA_SHA512RSA;

		// Determine usage policy
		CERT_POLICY_ID clientAuthPolicy;
		clientAuthPolicy.cCertPolicyElementId = 1;
		clientAuthPolicy.rgpszCertPolicyElementId =
			(LPSTR*)&szOID_PKIX_KP_CLIENT_AUTH;
		CERT_KEY_USAGE_RESTRICTION_INFO ckuri;
		ckuri.cCertPolicyId = 1;
		ckuri.rgCertPolicyId = &clientAuthPolicy;

		// Encode the usage policy information
		clientAuthenticationExtension = ToCertExtension(
			szOID_KEY_USAGE_RESTRICTION, &ckuri, TRUE);

		// Get the public key
		DWORD dwLength = 0;
		CryptExportPublicKeyInfo(
			hProvider,
			AT_KEYEXCHANGE,
			X509_ASN_ENCODING | PKCS_7_ASN_ENCODING,
			NULL,
			&dwLength);
		LPBYTE pPublicKey = new BYTE[dwLength];
		PCERT_PUBLIC_KEY_INFO pKeyInfo = reinterpret_cast<PCERT_PUBLIC_KEY_INFO>(pPublicKey);
		CryptExportPublicKeyInfo(
			hProvider,
			AT_KEYEXCHANGE,
			X509_ASN_ENCODING | PKCS_7_ASN_ENCODING,
			pKeyInfo,
			&dwLength);

		issuerUniqueIdBlob = StringToHashedBitBlob(signingCert->Issuer);
		subjectUniqueIdBlob = StringToHashedBitBlob(x500);

		CERT_INFO certInfo;
		certInfo.dwVersion = CERT_V3;
		certInfo.SerialNumber = serialNumberBlob;
		certInfo.SignatureAlgorithm = algid;
		certInfo.Issuer = issuerNameBlob;
		certInfo.Subject = subjectNameBlob;
		certInfo.NotBefore = startSystemTime;
		certInfo.NotAfter = endSystemTime;
		certInfo.SubjectPublicKeyInfo = *pKeyInfo;
		certInfo.IssuerUniqueId = issuerUniqueIdBlob;
		certInfo.SubjectUniqueId = subjectUniqueIdBlob;
		certInfo.cExtension = 1;
		certInfo.rgExtension = &clientAuthenticationExtension;

		dwLength = 0;
		CryptSignAndEncodeCertificate(
			hProvider,
			0,
			X509_ASN_ENCODING | PKCS_7_ASN_ENCODING,
			X509_CERT_TO_BE_SIGNED,
			&certInfo,
			&algid,
			NULL,
			NULL,
			&dwLength);
		LPBYTE pData = new BYTE[dwLength];
		CryptSignAndEncodeCertificate(
			hProvider,
			0,
			X509_ASN_ENCODING | PKCS_7_ASN_ENCODING,
			X509_CERT_TO_BE_SIGNED,
			&certInfo,
			&algid,
			NULL,
			pData,
			&dwLength);

		hCertContext = CertCreateCertificateContext(
			X509_ASN_ENCODING | PKCS_7_ASN_ENCODING,
			pData,
			dwLength);

		hCertStore = CertOpenStore(
			"Memory", 0, NULL, CERT_STORE_CREATE_NEW_FLAG, NULL);
		Check(hCertStore != NULL);

		Check(CertAddCertificateContextToStore(
			hCertStore,
			hCertContext,
			CERT_STORE_ADD_NEW,
			&hStoreCertContext));

		CertSetCertificateContextProperty(
			hStoreCertContext,
			CERT_KEY_PROV_INFO_PROP_ID,
			0,
			&kpi);

		//CertVerifySubjectCertificateContext

		if (password != nullptr)
		{
			passwordPtr = System::Runtime::InteropServices::Marshal::SecureStringToCoTaskMemUnicode(password);
		}

		CRYPT_DATA_BLOB pfxBlob;
		Check(PFXExportCertStoreEx(
			hCertStore,
			&pfxBlob,
			(LPCWSTR)passwordPtr.ToPointer(),
			NULL,
			EXPORT_PRIVATE_KEYS | REPORT_NO_PRIVATE_KEY | REPORT_NOT_ABLE_TO_EXPORT_PRIVATE_KEY));

		array<Byte>^ pfxData = gcnew array<Byte>(pfxBlob.cbData);
		pin_ptr<Byte> ppfxData = &pfxData[0];
		pfxBlob.pbData = ppfxData;
		Check(PFXExportCertStoreEx(
			hCertStore,
			&pfxBlob,
			(LPCWSTR)passwordPtr.ToPointer(),
			NULL,
			EXPORT_PRIVATE_KEYS | REPORT_NO_PRIVATE_KEY | REPORT_NOT_ABLE_TO_EXPORT_PRIVATE_KEY));

		return pfxData;
	}
	finally
	{
		FreeCryptIntegerBlob(serialNumberBlob);
		FreeCertNameBlob(subjectNameBlob);
		FreeCertNameBlob(issuerNameBlob);
		FreeBitBlob(issuerUniqueIdBlob);
		FreeBitBlob(subjectUniqueIdBlob);
		FreeCertExtension(clientAuthenticationExtension);
		if(passwordPtr != IntPtr::Zero)
		{
			System::Runtime::InteropServices::Marshal::ZeroFreeCoTaskMemUnicode(passwordPtr);
		}
		if(hCertContext != NULL)
		{
			CertFreeCertificateContext(hCertContext);
		}
		if(hStoreCertContext != NULL)
		{
			CertFreeCertificateContext(hStoreCertContext);
		}
		if(hCertStore != NULL)
		{
			CertCloseStore(hCertStore, 0);
		}
		if(hKey != NULL)
		{
			CryptDestroyKey(hKey);
		}
		if(hProvider != NULL)
		{
			CryptReleaseContext(hProvider, 0);
			CryptAcquireContext(
				&hProvider,
				wszContainerName,
				NULL,
				PROV_RSA_FULL,
				CRYPT_DELETEKEYSET);
		}
	}
}

array<Byte>^ Certificate::CreateSignedCertificateRevocationList(
	X509Certificate2^ signingCert,
	int serialNumber,
	DateTime nextUpdate,
	bool isDelta,
	int baseCrlSerialNumber,
	array<CrlEntry^>^ entries)
{
	FILETIME thisUpdateFileTime = ToFileTime(DateTime::UtcNow);
	FILETIME nextUpdateFileTime = ToFileTime(nextUpdate);
	String^ containerName = Guid::NewGuid().ToString();

	pin_ptr<const wchar_t> wszContainerName = PtrToStringChars(containerName);
	HCRYPTPROV hProvider = NULL;
	PCCERT_CONTEXT hCertContext = NULL;
	PCCERT_CONTEXT hStoreCertContext = NULL;
	HCERTSTORE hCertStore = NULL;
	CERT_NAME_BLOB issuerNameBlob;
	std::vector<CERT_EXTENSION> extensionsToFree;
	try
	{
		// Acquire crypto context
		Check(CryptAcquireContext(
			&hProvider,
			wszContainerName,
			NULL,
			PROV_RSA_FULL,
			CRYPT_NEWKEYSET));

		// Open temporary store
		hCertStore = CertOpenStore(
			"Memory", 0, NULL, CERT_STORE_CREATE_NEW_FLAG, NULL);
		Check(hCertStore != NULL);

		// Create certificate context from signing certificate
		array<BYTE>^ rawCertData = signingCert->RawData;
		pin_ptr<BYTE> pRawCert = &rawCertData[0];
		hCertContext = CertCreateCertificateContext(
			X509_ASN_ENCODING | PKCS_7_ASN_ENCODING,
			pRawCert,
			rawCertData->Length);

		// Add certificate to the store
		Check(CertAddCertificateContextToStore(
			hCertStore,
			hCertContext,
			CERT_STORE_ADD_NEW,
			&hStoreCertContext));

		// Attempt to get signing key
		//CryptG

		issuerNameBlob = ToCertName(signingCert->Issuer);

		// Determine algorithm to use
		// TODO: Allow this to arrive via parameter
		CRYPT_ALGORITHM_IDENTIFIER algid;
		algid.pszObjId = szOID_RSA_SHA512RSA;

		CRL_INFO crlInfo;
		crlInfo.dwVersion = CRL_V2;
		crlInfo.SignatureAlgorithm = algid;
		crlInfo.Issuer = issuerNameBlob;
		crlInfo.ThisUpdate = thisUpdateFileTime;
		crlInfo.NextUpdate = nextUpdateFileTime;
		crlInfo.cCRLEntry = entries->Length;
		crlInfo.rgCRLEntry = new CRL_ENTRY[entries->Length];
		IEnumerator^ iter = entries->GetEnumerator();
		for(int index = 0; index < entries->Length; ++index)
		{
			iter->MoveNext();
			CrlEntry^ entry = (CrlEntry^)iter->Current;
			crlInfo.rgCRLEntry[index].SerialNumber = ToSerialNumberBlob(entry->SerialNumber);
			crlInfo.rgCRLEntry[index].RevocationDate = ToFileTime(entry->RevocationDate);

			// Setup CRL entry extension if we have a suitable reason code
			if(entry->RevocationReason != 0)
			{
				int reason = entry->RevocationReason;
				CERT_EXTENSION crlReasonExtension = ToCertExtension(
					szOID_CRL_REASON_CODE, &reason, FALSE);
				extensionsToFree.push_back(crlReasonExtension);
				crlInfo.rgCRLEntry[index].cExtension = 1;
				crlInfo.rgCRLEntry[index].rgExtension = &crlReasonExtension;
			}
		}

		// Setup CRL extensions
		if(isDelta)
		{
			crlInfo.cExtension = 2;
			crlInfo.rgExtension = new CERT_EXTENSION[2];

			CERT_EXTENSION deltaCrlExtension = ToCertExtension(
				szOID_DELTA_CRL_INDICATOR, &baseCrlSerialNumber, TRUE);
			extensionsToFree.push_back(deltaCrlExtension);
			crlInfo.rgExtension[1] = deltaCrlExtension;
		}
		else
		{
			crlInfo.cExtension = 1;
			crlInfo.rgExtension = new CERT_EXTENSION[1];
		}

		CERT_EXTENSION crlNumberExtension = ToCertExtension(
			szOID_CRL_NUMBER, &serialNumber, FALSE);
		extensionsToFree.push_back(crlNumberExtension);
		crlInfo.rgExtension[0] = crlNumberExtension;

		DWORD dwLength = 0;
		Check(CryptSignAndEncodeCertificate(
			hProvider,
			AT_KEYEXCHANGE,
			X509_ASN_ENCODING | PKCS_7_ASN_ENCODING,
			X509_CERT_CRL_TO_BE_SIGNED,
			&crlInfo,
			&algid,
			NULL,
			NULL,
			&dwLength));

		array<Byte>^ pfxData = gcnew array<Byte>(dwLength);
		pin_ptr<Byte> ppfxData = &pfxData[0];
		Check(CryptSignAndEncodeCertificate(
			hProvider,
			AT_KEYEXCHANGE,
			X509_ASN_ENCODING | PKCS_7_ASN_ENCODING,
			X509_CERT_CRL_TO_BE_SIGNED,
			&crlInfo,
			&algid,
			NULL,
			ppfxData,
			&dwLength));
		return pfxData;
	}
	finally
	{
		std::vector<CERT_EXTENSION>::iterator it;
		for(it = extensionsToFree.begin(); it != extensionsToFree.end(); ++it)
		{
			FreeCertExtension(*it);
		}
	}
}

CRYPT_BIT_BLOB Certificate::StringToHashedBitBlob(String^ text)
{
	// TODO: Produce cryptographic hash of the text
	pin_ptr<const wchar_t> wszChars = PtrToStringChars(text);
	CRYPT_BIT_BLOB blob;
	blob.cbData = text->Length * 2;
	blob.cUnusedBits = 0;
	blob.pbData = new BYTE[blob.cbData];
	memcpy(blob.pbData, &wszChars[0], blob.cbData);
	return blob;
}

void Certificate::FreeBitBlob(CRYPT_BIT_BLOB blob)
{
	if(blob.pbData != NULL)
	{
		delete blob.pbData;
		blob.pbData = NULL;
	}
}

CERT_EXTENSION Certificate::ToCertExtension(LPCSTR structType, void* pvStruct, BOOL fCritical)
{
	LPBYTE pPolicy = NULL;
	try
	{
		// Encode the usage policy information
		DWORD dwLength = 0;
		Check(CryptEncodeObject(
			X509_ASN_ENCODING | PKCS_7_ASN_ENCODING,
			structType,
			pvStruct,
			NULL,
			&dwLength));
		pPolicy = new BYTE[dwLength];
		Check(CryptEncodeObject(
			X509_ASN_ENCODING | PKCS_7_ASN_ENCODING,
			structType,
			pvStruct,
			pPolicy,
			&dwLength));

		CERT_EXTENSION extension;
		extension.pszObjId = (LPSTR)structType;
		extension.fCritical = fCritical;
		extension.Value.cbData = dwLength;
		extension.Value.pbData = pPolicy;
		return extension;
	}
	catch(Exception^)
	{
		if(pPolicy != NULL)
		{
			delete pPolicy;
		}
		throw;
	}
}

void Certificate::FreeCertExtension(CERT_EXTENSION extension)
{
	if(extension.Value.pbData != NULL)
	{
		delete extension.Value.pbData;
		extension.Value.pbData = NULL;
	}
}

CERT_NAME_BLOB Certificate::ToCertName(String^ x500)
{
	DWORD dwLength = 0;
	LPCWSTR pszError;
	pin_ptr<const wchar_t> wszName = PtrToStringChars(x500);
	if(!CertStrToName(
		X509_ASN_ENCODING | PKCS_7_ASN_ENCODING,
		wszName,
		CERT_X500_NAME_STR,
		NULL,
		NULL,
		&dwLength,
		&pszError))
	{
		IntPtr temp = IntPtr((void*)pszError);
		String^ error = System::Runtime::InteropServices::Marshal::PtrToStringUni(temp);
		throw gcnew ArgumentException(error);
	}

	LPBYTE pName = new BYTE[dwLength];
	if(!CertStrToName(
		X509_ASN_ENCODING | PKCS_7_ASN_ENCODING,
		wszName,
		CERT_X500_NAME_STR,
		NULL,
		pName,
		&dwLength,
		&pszError))
	{
		delete pName;
		IntPtr temp = IntPtr((void*)pszError);
		String^ error = System::Runtime::InteropServices::Marshal::PtrToStringUni(temp);
		throw gcnew ArgumentException(error);
	}

	CERT_NAME_BLOB nameBlob;
	nameBlob.cbData = dwLength;
	nameBlob.pbData = pName;
	return nameBlob;
}

void Certificate::FreeCertNameBlob(CERT_NAME_BLOB blob)
{
	if(blob.pbData != NULL)
	{
		delete blob.pbData;
		blob.pbData = NULL;
	}
}

CRYPT_INTEGER_BLOB Certificate::ToSerialNumberBlob(Guid serialNumber)
{
	CRYPT_INTEGER_BLOB result;
	result.cbData = 16;
	result.pbData = new BYTE[16];

	pin_ptr<BYTE> pbuffer = &(serialNumber.ToByteArray())[0];
	memcpy(result.pbData, pbuffer, 16);

	return result;
}

CRYPT_INTEGER_BLOB Certificate::ToSerialNumberBlob(__int64 serialNumber)
{
	CRYPT_INTEGER_BLOB result;
	result.cbData = 8;
	result.pbData = new BYTE[8];

	// Get byte array for the number
	MemoryStream^ stm = gcnew MemoryStream();
	try
	{
		BinaryWriter^ writer = gcnew BinaryWriter(stm);
		writer->Write(serialNumber);
		writer->Flush();

		pin_ptr<BYTE> pbuffer = &(stm->GetBuffer())[0];
		memcpy(result.pbData, pbuffer, 8);
	}
	finally
	{
		stm->Close();
	}

	// Optimisation #1: Strip trailing zeros
	while(result.pbData[result.cbData-1] == 0 && result.cbData > 1)
	{
		--result.cbData;
	}

	return result;
}

void Certificate::FreeCryptIntegerBlob(CRYPT_INTEGER_BLOB blob)
{
	if(blob.pbData != NULL)
	{
		delete blob.pbData;
		blob.pbData = NULL;
	}
}

FILETIME Certificate::ToFileTime(DateTime dateTime)
{
	__int64 filetime = dateTime.ToFileTimeUtc();
	return *(reinterpret_cast<FILETIME*>(&filetime));
}

SYSTEMTIME Certificate::ToSystemTime(DateTime dateTime)
{
	__int64 fileTime = dateTime.ToFileTimeUtc();
	SYSTEMTIME systemTime;
	FileTimeToSystemTime(reinterpret_cast<FILETIME*>(&fileTime), &systemTime);
	return systemTime;
}

void Certificate::Check(BOOL nativeCallSucceeded)
{
	if (!nativeCallSucceeded)
	{
		int error = System::Runtime::InteropServices::Marshal::GetHRForLastWin32Error();
		System::Runtime::InteropServices::Marshal::ThrowExceptionForHR(error);
	}
}
