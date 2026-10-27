    using System.Collections.Generic;
    using UnityEngine;

    public class DropRateManager : MonoBehaviour
    {
        [System.Serializable]
        public class Drops
        {
            public string name;
            public GameObject itemPrefap;
            public float dropRate;
        }

        public List<Drops> drops;

        public void DropItem()
        {
            if (drops == null || drops.Count == 0)
                return;

            float randomNumber = Random.Range(0f, 100f);

            List<Drops> possibleDrops = new List<Drops>();

            foreach (Drops rate in drops)
            {
                if (randomNumber <= rate.dropRate)
                {
                    possibleDrops.Add(rate);
                }
            }

            if (possibleDrops.Count > 0)
            {
                Drops drop =
                    possibleDrops[Random.Range(0, possibleDrops.Count)];

                if (drop.itemPrefap == null)
                    return;

                Instantiate(
                    drop.itemPrefap,
                    transform.position,
                    Quaternion.identity
                );
            }
        }
    }
