using System;

namespace STXEnterprises.Host.Bridge;

public interface IHostView
{
	event Action<string>? MessageReceived;

	void PostJson(string json);
}
