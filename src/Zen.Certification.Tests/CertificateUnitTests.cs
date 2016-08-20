namespace Zen.Certification.Tests
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Security;
	using System.Security.Cryptography.X509Certificates;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class CertificateUnitTests
	{
		public static TestContext TestContext
		{
			get;
			private set;
		}

		/*[ClassInitialize]
		public static void ClassInitialize(TestContext testContext)
		{
			TestContext = testContext;
		}*/

		[TestMethod]
		public void IssueCertificate()
		{
			Certificate cert = new Certificate(
				Path.Combine(TestContext.TestDeploymentDir, "store.dat"));

			SecureString password = new SecureString();
			password.AppendChar('Z');
			password.AppendChar('e');
			password.AppendChar('n');
			password.AppendChar('!');

			byte[] payload = cert.CreateSelfSignedCertificate(
				"CN=\"Zen Design Corp CA\",OU=\"Network Security\",O=\"Zen Design Corp\",C=\"TH\"",
				DateTime.UtcNow,
				DateTime.UtcNow.AddYears(2),
				password,
				2048);
			X509Certificate2 wrappedCert =
				new X509Certificate2(payload, password);

			byte[] subCert = cert.CreateSignedCertificate(
				wrappedCert,
				Guid.NewGuid(),
				"CN=\"Test Client\",OU=\"Network Madness\",O=\"Zen Design Corp\",C=\"TH\"",
				DateTime.UtcNow,
				DateTime.UtcNow.AddYears(2),
				password,
				2048);
			X509Certificate2 wrappedSubCert =
				new X509Certificate2(subCert, password);

		}

		[TestMethod]
		public void IssueCACertificate()
		{
			Certificate cert = new Certificate(
				Path.Combine(TestContext.TestDeploymentDir, "store.dat"));

			SecureString password = new SecureString();
			password.AppendChar('Z');
			password.AppendChar('e');
			password.AppendChar('n');
			password.AppendChar('!');

			byte[] payload = cert.CreateSelfSignedCACertificate(
				"CN=\"Zen Design Corp CA\",OU=\"Network Security\",O=\"Zen Design Corp\",C=\"TH\"",
				DateTime.UtcNow,
				DateTime.UtcNow.AddYears(2),
				password,
				2048,
				false,
				0,
				new Uri(@"http://kazuya:33791/crl/"));
			X509Certificate2 wrappedCert =
				new X509Certificate2(payload, password);

			byte[] subCert = cert.CreateSignedCertificate(
				wrappedCert,
				Guid.NewGuid(),
				"CN=\"Test Client\",OU=\"Network Madness\",O=\"Zen Design Corp\",C=\"TH\"",
				DateTime.UtcNow,
				DateTime.UtcNow.AddYears(2),
				password,
				2048);
			X509Certificate2 wrappedSubCert =
				new X509Certificate2(subCert, password);
		}

		[TestMethod]
		public void IssueCRL()
		{
			Certificate cert = new Certificate(
				Path.Combine(TestContext.TestDeploymentDir, "store.dat"));

			SecureString password = new SecureString();
			password.AppendChar('Z');
			password.AppendChar('e');
			password.AppendChar('n');
			password.AppendChar('!');

			byte[] payload = cert.CreateSelfSignedCACertificate(
				"CN=\"Zen Design Corp CA\",OU=\"Network Security\",O=\"Zen Design Corp\",C=\"TH\"",
				DateTime.UtcNow,
				DateTime.UtcNow.AddYears(2),
				password,
				2048,
				false,
				0,
				new Uri(@"http://kazuya:33791/crl/"));
			X509Certificate2 wrappedCert =
				new X509Certificate2(payload, password);

			List<CrlEntry> entries = new List<CrlEntry>();
			entries.Add(new CrlEntry(Guid.NewGuid(), DateTime.UtcNow.AddHours(-1)));
			//entries.Add(new CrlEntry(Guid.NewGuid(), DateTime.UtcNow.AddHours(-1), 2));
			cert.CreateSignedCertificateRevocationList(
				wrappedCert,
				1,
				DateTime.UtcNow.AddHours(1),
				false,
				0,
				entries.ToArray());
		}
	}
}
