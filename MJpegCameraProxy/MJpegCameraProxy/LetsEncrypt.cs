using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Certes;
using Certes.Acme;
using Certes.Pkcs;

namespace MJpegCameraProxy
{
	public static class LetsEncrypt
	{
		public class CertificateChallenge
		{
			public string domain;
			public string challengeToken;
			public string expectedResponse;

			public CertificateChallenge(string domain, string challengeToken, string expectedResponse)
			{
				this.domain = domain;
				this.challengeToken = challengeToken;
				this.expectedResponse = expectedResponse;
			}
		}
		/// <summary>
		/// Gets a certificate in pfx format for the specified domain names.  If there is an error, an exception is thrown.  
		/// </summary>
		/// <param name="emailAddress">The email address to use for authorization.</param>
		/// <param name="domains">The domain names to authorize.  If any domain fails authoriation, an exception is thrown and the certificate request is not completed.</param>
		/// <param name="pfx_password">The password to use for the pfx (certificate) file.  You will need to use this to open the pfx file later.</param>
		/// <param name="prepareForChallenge">This is called by the GetCertificate method once for each domain, specifying the challenge that will be sent and the expected response.  It is the caller's responsibility to ensure that when the challenge is received, the expected response is sent.</param>
		public static async Task<byte[]> GetCertificate(string emailAddress, string[] domains, string pfx_password, Action<CertificateChallenge> prepareForChallenge, Action<string> statusUpdate)
		{
			// Remove duplicates from domains array, preserving order.
			HashSet<string> domainSet = new HashSet<string>();
			List<string> domainList = new List<string>();
			foreach (string domain in domains)
				if (domainSet.Contains(domain))
					continue;
				else
				{
					domainSet.Add(domain);
					domainList.Add(domain);
				}
			domains = domainList.ToArray();
			if (domains.Length == 0)
				throw new ArgumentException("No domains specified", "domains");

			statusUpdate("Starting certificate renewal for domains \"" + string.Join("\", \"", domains));

			using (AcmeClient client = new AcmeClient(WellKnownServers.LetsEncrypt))
			{
				// Create new registration
				AcmeAccount account = await client.NewRegistraton("mailto:" + emailAddress);

				// Accept terms of services
				account.Data.Agreement = account.GetTermsOfServiceUri();
				account = await client.UpdateRegistration(account);


				foreach (string domain in domains)
				{
					statusUpdate("Authorizing domain " + domain);
					// Initialize authorization
					AuthorizationIdentifier authorizationIdentifier = new AuthorizationIdentifier();
					authorizationIdentifier.Type = AuthorizationIdentifierTypes.Dns;
					authorizationIdentifier.Value = domain;
					AcmeResult<Authorization> authz = await client.NewAuthorization(authorizationIdentifier);

					// Compute key authorization for http-01
					Challenge httpChallengeInfo = authz.Data.Challenges.Where(c => c.Type == ChallengeTypes.Http01).First();
					string keyAuthString = client.ComputeKeyAuthorization(httpChallengeInfo);

					// Do something to fullfill the challenge,
					// e.g. upload key auth string to well known path, or make changes to DNS
					prepareForChallenge(new CertificateChallenge(domain, httpChallengeInfo.Token, keyAuthString));

					// Invite ACME server to validate the identifier
					AcmeResult<Challenge> httpChallenge = await client.CompleteChallenge(httpChallengeInfo);

					// Check authorization status
					authz = await client.GetAuthorization(httpChallenge.Location);
					Stopwatch sw = new Stopwatch();
					sw.Start();
					while (authz.Data.Status == EntityStatus.Pending)
					{
						if (sw.Elapsed > TimeSpan.FromMinutes(5))
							throw new Exception("Timed out waiting for domain \"" + domain + "\" authorization");
						// Wait for ACME server to validate the identifier
						await Task.Delay(10000);
						authz = await client.GetAuthorization(httpChallenge.Location);
					}

					if (authz.Data.Status != EntityStatus.Valid)
						throw new Exception("Failed to authorize domain \"" + domain + "\"");
				}
				statusUpdate("Authorization complete. Creating certificate.");

				// Create certificate
				CertificationRequestBuilder csr = new CertificationRequestBuilder();
				for (int i = 0; i < domains.Length; i++)
				{
					if (i == 0)
						csr.AddName("CN", domains[i]);
					else
						csr.SubjectAlternativeNames.Add(domains[i]);
				}

				AcmeCertificate cert = await client.NewCertificate(csr);

				// Export Pfx
				PfxBuilder pfxBuilder = cert.ToPfx();
				byte[] pfx = pfxBuilder.Build("LetsEncryptAuto", pfx_password);
				return pfx;
			}
		}
	}
}
