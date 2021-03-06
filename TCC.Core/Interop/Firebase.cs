﻿using Nostrum;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using TCC.Interop.Proxy;
using TCC.Utils;

namespace TCC.Interop
{
    public static class Firebase
    {
        private static readonly List<string> _registeredWebhooks = new List<string>();
        public static async void RegisterWebhook(string? webhook, bool online)
        {
            if (string.IsNullOrEmpty(webhook)) return;
            if (string.IsNullOrEmpty(App.Settings.LastAccountNameHash)) return;
            var req = new JObject
            {
                {"webhook", HashUtils.GenerateHash(webhook)},
                {"user", App.Settings.LastAccountNameHash},
                {"online", online }
            };
            using var c = MiscUtils.GetDefaultWebClient();
            c.Headers.Add(HttpRequestHeader.ContentType, "application/json");
            c.Headers.Add(HttpRequestHeader.AcceptCharset, "utf-8");
            c.Encoding = Encoding.UTF8;
            try
            {
                await c.UploadStringTaskAsync(
                    new Uri("http://us-central1-tcc-global-events.cloudfunctions.net/register_webhook"),
                    Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(req.ToString())));
                if (online)
                    _registeredWebhooks.Add(webhook);
                else
                    _registeredWebhooks.Remove(webhook);
            }
            catch
            {
                Log.F($"Failed to register webhook.");
            }
        }
        public static async Task<bool> RequestWebhookExecution(string webhook)
        {
            bool canFire;
            var req = new JObject
            {
                { "webhook" , HashUtils.GenerateHash(webhook)},
                { "user", App.Settings.LastAccountNameHash }
            };
            using var c = MiscUtils.GetDefaultWebClient();
            c.Headers.Add(HttpRequestHeader.ContentType, "application/json");
            c.Headers.Add(HttpRequestHeader.AcceptCharset, "utf-8");
            c.Encoding = Encoding.UTF8;
            try
            {
                var res = await c.UploadStringTaskAsync(
                    new Uri("http://us-central1-tcc-global-events.cloudfunctions.net/fire_webhook"),
                    Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(req.ToString())));

                var jRes = JObject.Parse(res);
                canFire = (jRes["canFire"] ?? throw new InvalidOperationException("Invalid webhook response format"))
                    .Value<bool>();
                Log.CW($"Webhook fire requested, result: {canFire}");

            }
            catch (WebException e)
            {
                Log.F($"Failed to request webhook execution. Webhook will be executed anyway: {e}");
                canFire = true;
            }
            catch (InvalidOperationException e)
            {
                Log.F($"Failed to request webhook execution. Webhook will be executed anyway: {e}");
                canFire = true;
            }

            return canFire;
        }

        public static async void SendUsageStatAsync()
        {
            if (App.Settings.StatSentVersion == App.AppVersion &&
                App.Settings.StatSentTime.Month == DateTime.UtcNow.Month &&
                App.Settings.StatSentTime.Day == DateTime.UtcNow.Day) return;

            try
            {
                using var c = MiscUtils.GetDefaultWebClient();
                c.Headers.Add(HttpRequestHeader.ContentType, "application/json");
                c.Headers.Add(HttpRequestHeader.AcceptCharset, "utf-8");
                c.Encoding = Encoding.UTF8;

                var js = new JObject
                {
                    {"region", Game.Server.Region},
                    {"server", Game.Server.ServerId},
                    {"account", App.Settings.LastAccountNameHash},
                    {"tcc_version", App.AppVersion},
                    {
                        "updated", App.Settings.StatSentTime.Month == DateTime.Now.Month &&
                                   App.Settings.StatSentTime.Day == DateTime.Now.Day &&
                                   App.Settings.StatSentVersion != App.AppVersion
                    },
                    {
                        "settings_summary", new JObject
                        {
                            {
                                "windows", new JObject
                                {
                                    { "cooldown", App.Settings.CooldownWindowSettings.Enabled },
                                    { "buffs", App.Settings.BuffWindowSettings.Enabled },
                                    { "character", App.Settings.CharacterWindowSettings.Enabled },
                                    { "class", App.Settings.ClassWindowSettings.Enabled },
                                    { "chat", App.Settings.ChatEnabled },
                                    { "group", App.Settings.GroupWindowSettings.Enabled }
                                }
                            },
                            {
                                "generic", new JObject
                                {
                                    { "proxy_enabled", StubInterface.Instance.IsStubAvailable},
                                    { "mode", App.ToolboxMode ? "toolbox" : "standalone" }
                                }
                            }
                        }
                    }
                };

                await c.UploadStringTaskAsync(new Uri("https://us-central1-tcc-usage-stats.cloudfunctions.net/usage_stat"),
                    Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(js.ToString())));

                App.Settings.StatSentTime = DateTime.UtcNow;
                App.Settings.StatSentVersion = App.AppVersion;
                App.Settings.Save();
            }
            catch
            {
                //TODO: write error?
            }
        }

        public static void Dispose()
        {
            try
            {
                var webhooks = new string[_registeredWebhooks.Count];
                _registeredWebhooks.CopyTo(webhooks);
                webhooks.ToList().ForEach(w =>
                {
                    RegisterWebhook(w, false);
                });
            }
            catch (Exception e)
            {
                Log.F($"Failed to dispose Firebase webhooks: {e}");
            }
        }
    }
}