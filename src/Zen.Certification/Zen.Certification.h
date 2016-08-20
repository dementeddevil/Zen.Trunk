// Zen.Certification.h

#pragma once

using namespace System;
using namespace System::Security;
using namespace System::Security::Cryptography::X509Certificates;

namespace Zen
{
	namespace Certification
	{
		public ref class CrlEntry
		{
		public:
			CrlEntry(Guid serialNumber, DateTime revocationDate)
			{
				_serialNumber = serialNumber;
				_revocationDate = revocationDate;
			}

			CrlEntry(Guid serialNumber, DateTime revocationDate, int revocationReason)
			{
				_serialNumber = serialNumber;
				_revocationDate = revocationDate;
				_revocationReason = revocationReason;
			}

			property Guid SerialNumber
			{
				Guid get()
				{
					return _serialNumber;
				};
			}

			property DateTime RevocationDate
			{
				DateTime get()
				{
					return _revocationDate;
				};
			}

			property int RevocationReason
			{
				int get()
				{
					return _revocationReason;
				};
			}

		private:
			Guid _serialNumber;
			DateTime _revocationDate;
			int _revocationReason;
		};

		public ref class Certificate
		{
		public:
			Certificate(String^ storePathName);

			array<Byte>^ CreateSelfSignedCertificate(
				String^ x500,
				DateTime startTime,
				DateTime endTime,
				SecureString^ password,
				int keyLength);

			array<Byte>^ CreateSelfSignedCACertificate(
				String^ x500,
				DateTime startTime,
				DateTime endTime,
				SecureString^ password,
				int keyLength,
				bool limitPathLength,
				int pathLengthConstraint,
				Uri^ crlDistributionUri);

			array<Byte>^ CreateSignedCertificate(
				X509Certificate2^ signingCert,
				Guid serialNumber,
				String^ x500,
				DateTime startTime,
				DateTime endTime,
				SecureString^ password,
				int keyLength);

			array<Byte>^ CreateSignedCertificateRevocationList(
				X509Certificate2^ signingCert,
				int serialNumber,
				DateTime nextUpdate,
				bool isDelta,
				int baseCrlSerialNumber,
				array<CrlEntry^>^ entries);

		private:
			CRYPT_BIT_BLOB StringToHashedBitBlob(String^ text);
			void FreeBitBlob(CRYPT_BIT_BLOB blob);
			CERT_EXTENSION ToCertExtension(LPCSTR structType, void* pvStruct, BOOL fCritical);
			void FreeCertExtension(CERT_EXTENSION extension);
			CERT_NAME_BLOB ToCertName(String^ x500);
			void FreeCertNameBlob(CERT_NAME_BLOB blob);
			CRYPT_INTEGER_BLOB ToSerialNumberBlob(Guid serialNumber);
			CRYPT_INTEGER_BLOB ToSerialNumberBlob(__int64 serialNumber);
			void FreeCryptIntegerBlob(CRYPT_INTEGER_BLOB blob);
			FILETIME ToFileTime(DateTime dateTime);
			SYSTEMTIME ToSystemTime(DateTime dateTime);
			void Check(BOOL nativeCallSucceeded);

			String^ _storePathName;
			HANDLE _storeHandle;
		};
	}
}
