using Rocket.API;

namespace DaeGirisLimitleyici
{
    public class GirişLimitleyiciYapılandırma : IRocketPluginConfiguration
    {
        public bool AtmadanÖnceBekle { get; set; }
        public float AtmaGecikmesi { get; set; }

        public bool HesapEskiliğiniKısıtla { get; set; }
        public int MinimumOluşturmaGünü { get; set; }
        
        public bool OyunSaatiniKısıtla { get; set; }
        public double MinimumOyunSaati { get; set; }

        public string BağlanmaYetkisi { get; set; }

        public void LoadDefaults()
        {
            AtmadanÖnceBekle = false;
            AtmaGecikmesi = 5.0f;

            HesapEskiliğiniKısıtla = true;
            MinimumOluşturmaGünü = 60;

            OyunSaatiniKısıtla = false;
            MinimumOyunSaati = 10.0;

            BağlanmaYetkisi = "Bağlanabilir";
        }
    }
}