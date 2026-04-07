using BarRaider.SdTools;
using Newtonsoft.Json.Linq;
using Websocket.Client;

namespace PhoenixFlareSDIntergration;

class Program
{
	static void Main(string[] args)
	{
		//System.Diagnostics.Debugger.Launch();
		SDWrapper.Run(args);
	}

	[PluginActionId("com.phoenix.tuya.action")]
	public class TuyaAction : PluginBase
	{
		private static readonly WebsocketClient _mauiClient = new(new Uri("ws://127.0.0.1:9020"));

		private JObject settings;

		public TuyaAction(ISDConnection connection, InitialPayload payload) : base(connection, payload)
		{
			// 1. Hook into messages coming from the Property Inspector (the UI)
			settings = payload.Settings;
			Connection.OnSendToPlugin += Connection_OnSendToPlugin;
			
			_mauiClient.ReconnectTimeout = TimeSpan.FromSeconds(30);
			_mauiClient.ErrorReconnectTimeout = TimeSpan.FromSeconds(5);

			_mauiClient.ReconnectionHappened.Subscribe(info =>
			{
				RequestStatus();
			});

			// 2. Setup the WebSocket listener once
			_mauiClient.MessageReceived.Subscribe(msg =>
			{
				if (Connection is not null) HandleMauiMessage(msg.Text);
			});

			if (!_mauiClient.IsRunning) _mauiClient.Start();

			// 3. Request current status immediately on startup so the bulb looks right
			RequestStatus();
		}

		private void RequestStatus()
		{
			var deviceId = settings["deviceId"]?.ToString();
			if (!string.IsNullOrEmpty(deviceId))
			{
				_mauiClient.Send($"GET_STATUS:{deviceId}");
			}
		}

		public override void KeyPressed(KeyPayload payload)
		{
			var deviceId = payload.Settings["deviceId"]?.ToString();
			if (!string.IsNullOrEmpty(deviceId))
			{
				// Send the toggle command to Phoenix Flare
				_mauiClient.SendInstant($"TOGGLE:{deviceId}");
			}
			else
			{
				Connection.ShowAlert(); // Show a '!' if no device is selected
			}
		}

		private void HandleMauiMessage(string message)
		{
			// Handle Device List for the Dropdown
			if (message.StartsWith("DEVICE_LIST:"))
			{
				var json = message.Replace("DEVICE_LIST:", "");
				Connection.SendToPropertyInspectorAsync(new JObject { ["devices"] = json });
			}
			// Handle Status Updates (This swaps the light bulb icon)
			else if (message.StartsWith("STATUS:"))
			{
				string[] parts = message.Split(':'); // Expecting STATUS:id:True/False
				string msgDeviceId = parts[1];
				bool isOn = Boolean.Parse(parts[2]);

				// Only update if this specific button is for this device
				if (settings["deviceId"]?.ToString() == msgDeviceId)
				{
					// State 0 = lightOff.png, State 1 = lightOn.png (as defined in manifest)
					Connection.SetStateAsync(isOn ? (uint)1 : 0);
				}
			}
		}

		private void Connection_OnSendToPlugin(object sender,
		                                       BarRaider.SdTools.Wrappers.SDEventReceivedEventArgs<
			                                       BarRaider.SdTools.Events.SendToPlugin> e)
		{
			if (e.Event.Payload["command"]?.ToString() == "FETCH_DEVICES_FROM_MAUI")
			{
				_mauiClient.Send("FETCH_DEVICES");
			}
		}

		public override void Dispose()
		{
			Connection.OnSendToPlugin -= Connection_OnSendToPlugin;
		}

		// Update to prevent any default behaviours
		public override void KeyReleased(KeyPayload payload)
		{
			Task.Run(async () =>
			{
				await Task.Delay(1350);
				RequestStatus();
			});
		}

		public override void OnTick()
		{
		}

		public override void ReceivedSettings(ReceivedSettingsPayload payload)
		{
			settings = payload.Settings;
			RequestStatus();
		}

		public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload)
		{
		}
	}
}