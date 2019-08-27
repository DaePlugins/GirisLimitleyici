using System;
using System.Collections;
using System.Linq;
using System.Net;
using System.Xml;
using Rocket.API;
using Rocket.API.Collections;
using Logger = Rocket.Core.Logging.Logger;
using Rocket.Core.Plugins;
using Rocket.Core.Steam;
using Rocket.Unturned;
using Rocket.Unturned.Player;
using UnityEngine;
using UnityEngine.Networking;

namespace DaeGirisLimitleyici
{
    public class GirişLimitleyici : RocketPlugin<GirişLimitleyiciYapılandırma>
    {
        protected override void Load()
        {
            ServicePointManager.ServerCertificateValidationCallback += (gönderen, sertifika, zincir, sslİlkeHataları) => true;

            if (Configuration.Instance.HesapEskiliğiniKısıtla || Configuration.Instance.OyunSaatiniKısıtla)
            {
                U.Events.OnPlayerConnected += OyuncuBağlandığında;
            }
        }

        protected override void Unload()
        {
            if (Configuration.Instance.HesapEskiliğiniKısıtla || Configuration.Instance.OyunSaatiniKısıtla)
            {
                U.Events.OnPlayerConnected -= OyuncuBağlandığında;

                StopAllCoroutines();
            }
        }

        private void OyuncuBağlandığında(UnturnedPlayer oyuncu)
        {
            if (!oyuncu.HasPermission($"dae.girislimitleyici.{Configuration.Instance.BağlanmaYetkisi}"))
            {
                StartCoroutine(Kontrol(oyuncu));
            }
        }

        private IEnumerator Kontrol(UnturnedPlayer oyuncu)
        {
            var doküman = new XmlDocument();

            using (var istek = UnityWebRequest.Get($"http://steamcommunity.com/profiles/{oyuncu.CSteamID.m_SteamID}?xml=1"))
            {
                istek.SetRequestHeader("Cache-Control", "max-age=0, no-cache, no-store");
                istek.SetRequestHeader("Pragma", "no-cache");

                yield return istek.SendWebRequest();

                if (istek.isNetworkError || istek.isHttpError)
                {
                    Logger.LogError(istek.error);
                    yield break;
                }

                doküman.LoadXml(istek.downloadHandler.text);
            }

            var profil = doküman["profile"];
            if (profil["privacyState"].ParseString() == "private")
            {
                if (Configuration.Instance.AtmadanÖnceBekle)
                {
                    yield return new WaitForSeconds(Configuration.Instance.AtmaGecikmesi);
                }

                oyuncu.Kick(Translate("GizliProfil"));
                yield break;
            }

            if (Configuration.Instance.HesapEskiliğiniKısıtla)
            {
                var oluşturmaGünü = DateTime.Parse(profil["memberSince"].ParseString());
                var hesapEskiliği = DateTime.UtcNow.Subtract(oluşturmaGünü).TotalDays;

                if (Configuration.Instance.MinimumOluşturmaGünü > hesapEskiliği)
                {
                    if (Configuration.Instance.AtmadanÖnceBekle)
                    {
                        yield return new WaitForSeconds(Configuration.Instance.AtmaGecikmesi);
                    }

                    oyuncu.Kick(Translate("HesapEskiliğiYetersiz", Math.Round(Configuration.Instance.MinimumOluşturmaGünü - hesapEskiliği, 2)));
                    yield break;
                }
            }

            if (Configuration.Instance.OyunSaatiniKısıtla)
            {
                var enÇokOynananOyunlar = profil["mostPlayedGames"];
                if (enÇokOynananOyunlar == null)
                {
                    if (Configuration.Instance.AtmadanÖnceBekle)
                    {
                        yield return new WaitForSeconds(Configuration.Instance.AtmaGecikmesi);
                    }

                    oyuncu.Kick(Translate("OyunSaatleriGizli"));
                    yield break;
                }

                var oyun = enÇokOynananOyunlar.ChildNodes
                    .Cast<XmlElement>()
                    .FirstOrDefault(e => e["gameName"].ParseString() == "Unturned");
                if (oyun == null)
                {
                    if (Configuration.Instance.AtmadanÖnceBekle)
                    {
                        yield return new WaitForSeconds(Configuration.Instance.AtmaGecikmesi);
                    }

                    oyuncu.Kick(Translate("SonOynanılanlardaYok"));
                    yield break;
                }

                var oyunSaati = oyun["hoursPlayed"].ParseDouble();
                if (Configuration.Instance.MinimumOyunSaati > oyunSaati.Value)
                {
                    if (Configuration.Instance.AtmadanÖnceBekle)
                    {
                        yield return new WaitForSeconds(Configuration.Instance.AtmaGecikmesi);
                    }

                    oyuncu.Kick(Translate("OyunSaatiYetersiz", Math.Round(Configuration.Instance.MinimumOyunSaati - oyunSaati.Value, 2)));
                }
            }
        }

        public override TranslationList DefaultTranslations => new TranslationList
        {
            { "GizliProfil", "Profilin gizliyken bu sunucuya giremezsin." },
            { "HesapEskiliğiYetersiz", "Hesabının {0} gün daha eski olması gerekiyor." },
            { "OyunSaatleriGizli", "Oyun saatlerin gizliyken bu sunucuya giremezsin." },
            { "SonOynanılanlardaYok", "Son oynanılan oyunlar arasında Unturned yok." },
            { "OyunSaatiYetersiz", "Oyunda {0} saat daha geçirmen gerekiyor." }
        };
    }
}