using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MJpegCameraProxy.Configuration;
using System.Threading;
using BPUtil;

namespace MJpegCameraProxy.PanTiltZoom.Custom
{
	public class CustomPTZProfile : IPTZSimple, IPTZHtml, IPTZPresets
	{
		CameraSpec camSpec;
		IPCameraBase cam;
		string urlBase;

		public CustomPTZProfile(CameraSpec camSpec, IPCameraBase cam)
		{
			this.camSpec = camSpec;
			this.cam = cam;
			urlBase = "http://" + camSpec.ptz_hostName + "/";
		}

		#region IPTZHtml Members

		public string GetHtml(string camId, IPCameraBase cam)
		{
			PTZProfile profile = GetProfile();
			if (profile == null)
				return "PTZ Profile Not Found";
			if (typeof(PTZSpecV1) == profile.spec.GetType())
			{
				PTZSpecV1 spec = (PTZSpecV1)profile.spec;

				HtmlOptions options = new HtmlOptions();
				options.showPtzArrows = true;
				options.showPtzDiagonals = spec.EnableDiagonals;

				options.panDelay = (int)(spec.PanRunTimeMs * 0.9);
				options.tiltDelay = (int)(spec.TiltRunTimeMs * 0.9);

				options.showZoomButtons = spec.EnableZoom;

				options.showZoomInLong = !string.IsNullOrWhiteSpace(spec.zoomInLong);
				options.showZoomInMedium = !string.IsNullOrWhiteSpace(spec.zoomInMedium);
				options.showZoomInShort = !string.IsNullOrWhiteSpace(spec.zoomInShort);

				options.showZoomOutLong = !string.IsNullOrWhiteSpace(spec.zoomOutLong);
				options.showZoomOutMedium = !string.IsNullOrWhiteSpace(spec.zoomOutMedium);
				options.showZoomOutShort = !string.IsNullOrWhiteSpace(spec.zoomOutShort);

				options.zoomShortDelay = spec.ZoomRunTimeShortMs;
				options.zoomMediumDelay = spec.ZoomRunTimeMediumMs;
				options.zoomLongDelay = spec.ZoomRunTimeLongMs;

				options.showPresets = spec.EnablePresets;
				int i = 0;
				options.gettablePresets[i++] = !string.IsNullOrWhiteSpace(spec.load_preset_1);
				options.gettablePresets[i++] = !string.IsNullOrWhiteSpace(spec.load_preset_2);
				options.gettablePresets[i++] = !string.IsNullOrWhiteSpace(spec.load_preset_3);
				options.gettablePresets[i++] = !string.IsNullOrWhiteSpace(spec.load_preset_4);
				options.gettablePresets[i++] = !string.IsNullOrWhiteSpace(spec.load_preset_5);
				options.gettablePresets[i++] = !string.IsNullOrWhiteSpace(spec.load_preset_6);
				options.gettablePresets[i++] = !string.IsNullOrWhiteSpace(spec.load_preset_7);
				options.gettablePresets[i++] = !string.IsNullOrWhiteSpace(spec.load_preset_8);
				i = 0;
				options.settablePresets[i++] = !string.IsNullOrWhiteSpace(spec.save_preset_1);
				options.settablePresets[i++] = !string.IsNullOrWhiteSpace(spec.save_preset_2);
				options.settablePresets[i++] = !string.IsNullOrWhiteSpace(spec.save_preset_3);
				options.settablePresets[i++] = !string.IsNullOrWhiteSpace(spec.save_preset_4);
				options.settablePresets[i++] = !string.IsNullOrWhiteSpace(spec.save_preset_5);
				options.settablePresets[i++] = !string.IsNullOrWhiteSpace(spec.save_preset_6);
				options.settablePresets[i++] = !string.IsNullOrWhiteSpace(spec.save_preset_7);
				options.settablePresets[i++] = !string.IsNullOrWhiteSpace(spec.save_preset_8);

				options.showZoomLevels = false;

				return PTZHtml.GetHtml(camId, cam, options);
			}
			return "Unsupported PTZ Profile";
		}

		#endregion

		#region IPTZSimple Members

		public void MoveSimple(PTZDirection direction)
		{
			PTZProfile profile = GetProfile();
			if (profile == null)
				return;
			if(typeof(PTZSpecV1) == profile.spec.GetType())
			{
				PTZSpecV1 spec = (PTZSpecV1)profile.spec;
				string url;
				int waitTime;
				if (direction == PTZDirection.Up)
				{
					waitTime = spec.TiltRunTimeMs;
					url = spec.up;
				}
				else if (direction == PTZDirection.Down)
				{
					waitTime = spec.TiltRunTimeMs;
					url = spec.down;
				}
				else if (direction == PTZDirection.Left)
				{
					waitTime = spec.PanRunTimeMs;
					url = spec.left;
				}
				else if (direction == PTZDirection.Right)
				{
					waitTime = spec.PanRunTimeMs;
					url = spec.right;
				}
				else if (spec.EnableDiagonals)
				{
					if (direction == PTZDirection.UpLeft)
						url = spec.upleft;
					else if (direction == PTZDirection.DownLeft)
						url = spec.downleft;
					else if (direction == PTZDirection.UpRight)
						url = spec.upright;
					else if (direction == PTZDirection.DownRight)
						url = spec.downright;
					else
						return;

					waitTime = Math.Min(spec.PanRunTimeMs, spec.TiltRunTimeMs);
				}
				else
					return;

				SimpleProxy.GetData(urlBase + url, camSpec.ptz_username, camSpec.ptz_password);

				if (spec.SendStopCommandAfterPanTilt && !string.IsNullOrWhiteSpace(spec.stopPanTilt))
				{
					Thread.Sleep(waitTime);
					SimpleProxy.GetData(urlBase + spec.stopPanTilt, camSpec.ptz_username, camSpec.ptz_password);
				}
			}
		}

		public bool SupportsZoom()
		{
			PTZProfile profile = GetProfile();
			if (profile == null)
				return false;
			return ((PTZSpecV1)profile.spec).EnableZoom;
		}

		public void Zoom(ZoomDirection direction, ZoomAmount amount)
		{
			PTZProfile profile = GetProfile();
			if (profile == null)
				return;
			if (typeof(PTZSpecV1) == profile.spec.GetType())
			{
				PTZSpecV1 spec = (PTZSpecV1)profile.spec;
				string url = null;
				int waitTime = 0;
				if (direction == ZoomDirection.In)
				{
					if (amount == ZoomAmount.Short && !string.IsNullOrWhiteSpace(spec.zoomInShort))
					{
						waitTime = spec.ZoomRunTimeShortMs;
						url = spec.zoomInShort;
					}
					else if (amount == ZoomAmount.Medium && !string.IsNullOrWhiteSpace(spec.zoomInMedium))
					{
						waitTime = spec.ZoomRunTimeMediumMs;
						url = spec.zoomInMedium;
					}
					else if (amount == ZoomAmount.Long && !string.IsNullOrWhiteSpace(spec.zoomInLong))
					{
						waitTime = spec.ZoomRunTimeLongMs;
						url = spec.zoomInLong;
					}
				}
				else if (direction == ZoomDirection.Out)
				{
					if (amount == ZoomAmount.Short && !string.IsNullOrWhiteSpace(spec.zoomOutShort))
					{
						waitTime = spec.ZoomRunTimeShortMs;
						url = spec.zoomOutShort;
					}
					else if (amount == ZoomAmount.Medium && !string.IsNullOrWhiteSpace(spec.zoomOutMedium))
					{
						waitTime = spec.ZoomRunTimeMediumMs;
						url = spec.zoomOutMedium;
					}
					else if (amount == ZoomAmount.Long && !string.IsNullOrWhiteSpace(spec.zoomOutLong))
					{
						waitTime = spec.ZoomRunTimeLongMs;
						url = spec.zoomOutLong;
					}
				}
				if (string.IsNullOrWhiteSpace(url))
					return;

				SimpleProxy.GetData(urlBase + url, camSpec.ptz_username, camSpec.ptz_password);

				if (spec.SendStopCommandAfterZoom && !string.IsNullOrWhiteSpace(spec.stopZoom))
				{
					Thread.Sleep(waitTime);
					SimpleProxy.GetData(urlBase + spec.stopZoom, camSpec.ptz_username, camSpec.ptz_password);
				}
			}
		}

		#endregion

		#region IPTZPresets Members

		public void LoadPreset(int presetNum)
		{
			Preset(presetNum, false);
		}

		public void SavePreset(int presetNum)
		{
			Preset(presetNum, true);
		}

		#endregion

		private void Preset(int presetNum, bool save)
		{
			PTZProfile profile = GetProfile();
			if (profile == null)
				return;
			if (typeof(PTZSpecV1) == profile.spec.GetType())
			{
				PTZSpecV1 spec = (PTZSpecV1)profile.spec;
				string url = null;
				if (presetNum == 1)
					url = save ? spec.save_preset_1 : spec.load_preset_1;
				else if (presetNum == 2)
					url = save ? spec.save_preset_2 : spec.load_preset_2;
				else if (presetNum == 3)
					url = save ? spec.save_preset_3 : spec.load_preset_3;
				else if (presetNum == 4)
					url = save ? spec.save_preset_4 : spec.load_preset_4;
				else if (presetNum == 5)
					url = save ? spec.save_preset_5 : spec.load_preset_5;
				else if (presetNum == 6)
					url = save ? spec.save_preset_6 : spec.load_preset_6;
				else if (presetNum == 7)
					url = save ? spec.save_preset_7 : spec.load_preset_7;
				else if (presetNum == 8)
					url = save ? spec.save_preset_8 : spec.load_preset_8;
				if (!string.IsNullOrWhiteSpace(url))
				{
					try
					{
						byte[] image = MJpegServer.cm.GetLatestImage(cam.cameraSpec.id);
						if (image.Length > 0)
							Util.WriteImageThumbnailToFile(image, CameraProxyGlobals.ThumbsDirectoryBase + cam.cameraSpec.id.ToLower() + presetNum + ".jpg");
					}
					catch (Exception ex)
					{
						Logger.Debug(ex);
					}
					SimpleProxy.GetData(urlBase + url, camSpec.ptz_username, camSpec.ptz_password);
				}
			}
		}

		private PTZProfile GetProfile()
		{
			if (camSpec.ptzType != PtzType.CustomPTZProfile)
				return null;
			PTZProfile profile = new PTZProfile();
			profile.Load(CameraProxyGlobals.PTZProfilesDirectoryBase + camSpec.ptz_customPTZProfile + ".xml");
			if (profile.spec == null)
				profile = null;
			return profile;
		}
	}
}
