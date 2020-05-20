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

        public void Start()
        {
            spawners = new IFoodSpawner[plantSpawners.Length];
            for (int i = 0; i < plantSpawners.Length; i++)
            {
                spawners[i] = new GuassianFoodSpawner(plantSpawners[i].transform.position, 100);
            }
        }

        public void Update()
        {
            var plants = GameObject.FindGameObjectsWithTag("plant");
            if(plants.Length < plant_limit)
            {

                SpawnFood();
            }
        }

        public void SpawnFood()
        {
            foreach(IFoodSpawner spawner in spawners)
            {
                var foodPos = spawner.SpawnFood(Time.deltaTime, GameObject.FindGameObjectsWithTag("plant"));
                if (foodPos != Vector3.zero)
                {
                    var newFood = Instantiate(plantFood, foodPos, plantFood.transform.localRotation);
                }
            }
            
        }

    }
}
