using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts
{
    public class TerrainManager : MonoBehaviour
    {
        private IFoodSpawner[] spawners;
        public GameObject plantFood;
        public GameObject[] plantSpawners;

        private int plant_limit = 50;
        private Vector3 spawnerPos;

        public void Start()
        {
            spawners = new IFoodSpawner[plantSpawners.Length];
            for (int i = 0; i < plantSpawners.Length; i++)
            {
                spawners[i] = new GuassianFoodSpawner(plantSpawners[i].transform.position, 50);
            }
            spawnerPos = plantSpawners[0].transform.position;
        }

        public void Update()
        {
            var plants = GameObject.FindGameObjectsWithTag("plant");
            if (plants.Length < plant_limit)
            {
                SpawnFood();
            }
            // Dynamic position
            MovePlantSpawners();
        }

        public void SpawnFood()
        {
            foreach (IFoodSpawner spawner in spawners)
            {
                var foodPos = spawner.SpawnFood(Time.deltaTime, GameObject.FindGameObjectsWithTag("plant"));
                if (foodPos != Vector3.zero)
                {
                    var newFood = Instantiate(plantFood, foodPos, plantFood.transform.localRotation);
                    Destroy(newFood, 100f);
                }
            }

        }

        private void MovePlantSpawners()
        {
            Vector3 pos = plantSpawners[0].transform.position;
            var rx = UnityEngine.Random.Range(-2.0f, 2.0f);
            var rz = UnityEngine.Random.Range(-2.0f, 2.0f);
            float dx = 0;
            float dz = 0;

            if (rx > 0 && pos.x < spawnerPos.x + 300f)
                dx = rx;
            if (rx < 0 && pos.x > spawnerPos.x - 300f)
                dx = rx;
            if (rz > 0 && pos.z < spawnerPos.z + 300f)
                dz = rz;
            if (rz < 0 && pos.z > spawnerPos.z - 300f)
                dz = rz;

            plantSpawners[0].transform.position = new Vector3(pos.x + dx, 0f, pos.z + dz);
        }

    }
}