using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Grpc.Core;
using Microsoft.Win32;
using ResurrectedTrade.AgentBase;
using ResurrectedTrade.Protocol.Profile;

namespace ResurrectedTrade.Agent
{
    public partial class LoginPrompt : Form
    {
        private readonly ProfileService.ProfileServiceClient _client;

        public LoginPrompt(ProfileService.ProfileServiceClient client)
        {
            _client = client;
            InitializeComponent();
            Username.Text = (string)Utils.AgentRegistryKey.GetValue("USERNAME", "");
        }

        private void RegisterClick(object sender, EventArgs e)
        {
            Utils.OpenUrl("https://resurrected.trade/auth?t=1");
        }

        private async void LoginClick(object sender, EventArgs e)
        {
            Username.Enabled = false;
            Password.Enabled = false;
            Login.Enabled = false;
            try
            {
                Utils.AgentRegistryKey.SetValue(
                    "USERNAME", Username.Text,
                    RegistryValueKind.String
                );

                var response = await Task.Run(
                    () => _client.Login(
                        new LoginRequest
                        {
                            UserId = Username.Text,
                            Password = Password.Text
                        }
                    )
                );
                if (response.Success)
                {
                    Close();
                    DialogResult = DialogResult.OK;
                    return;
                }

                var errors = "Failed to log in:\n\n" + string.Join(
                    "\n", response.Errors.Select(o => $"{o.Code}: {o.Description}")
                );

                UIUtils.ShowError("Authentication failed", errors);
            }
            catch (Exception exc)
            {
                if (Utils.IsStatusCodeException(exc, StatusCode.Unavailable))
                {
                    UIUtils.ShowError("Server unavailable", "Resurrect trade is down");
                }
                else
                {
                    UIUtils.ShowException("Failed to contact login server", exc);
                }
            }
            finally
            {
                Username.Enabled = true;
                Password.Enabled = true;
                Login.Enabled = true;
            }
        }

        private void InputTextChanged(object sender, EventArgs e)
        {
            Login.Enabled = Username.Text.Length > 0 && Password.Text.Length > 0;
        }
    }
}
