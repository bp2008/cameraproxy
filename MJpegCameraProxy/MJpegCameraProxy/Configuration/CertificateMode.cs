using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MJpegCameraProxy.Configuration
{
	public enum CertificateMode
	{
		SelfSigned,
		PfxFile,
		LetsEncrypt
	}
}
