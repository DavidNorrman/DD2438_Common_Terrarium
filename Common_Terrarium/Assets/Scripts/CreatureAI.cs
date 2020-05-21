using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using MLAgents;
using MLAgents.Sensors;

namespace Assets.Scripts
{
    public class CreatureAI : Agent
    {
        private Creature creature;
        public Vector3 spawnArea;
        public Vector3 foodInstinct;
        public bool child = false;

        public void Start()
        {
            Debug.Log($"Creature AI is ready");
            creature = GetComponent<Creature>();
            spawnArea = transform.localPosition;
            if(!child)
                foodInstinct = GameObject.Find("PlantSpawn").transform.position;
        }

        // Returns direction and distance to foodInstinct
        public (Vector3, float) UseFoodInstinct()
        {
            Vector3 heading = foodInstinct - transform.position;
            float distance = heading.magnitude;
            Vector3 direction = heading / distance;

            Vector3 foodDirectionInstinct = direction;
            float foodDistanceInstinct = distance;

            return (direction, distance);
        }

        private (GameObject, bool) FindFood()
        {
            List<GameObject> food = new List<GameObject>();
            if (creature.CreatureRegime == Creature.Regime.HERBIVORE)
                food = creature.Sensor.SensePlants(creature);
            else if (creature.CreatureRegime == Creature.Regime.CARNIVORE)
                food = creature.Sensor.SensePreys(creature);
            Vector3 closestFood = Vector3.zero;
            float bestDistance = Vector3.Distance(closestFood, transform.position);
            GameObject closestFoodObj = new GameObject("empty");
            if (food.Count == 0)
                return ((closestFoodObj, false));
            foreach (var foodPiece in food)
            {
                if (Vector3.Distance(foodPiece.transform.position, transform.position) < bestDistance)
                {
                    bestDistance = Vector3.Distance(foodPiece.transform.position, transform.position);
                    closestFood = foodPiece.transform.position;
                    closestFoodObj = foodPiece;
                }
            }
            return (closestFoodObj, true);
        }

        public virtual void OnAccessibleFood(GameObject food)
        {
            creature.Eat(food);
            //if (creature.Energy > 0.2 * creature.MaxEnergy && UnityEngine.Random.Range(0, 1) < 0.1f) creature.Reproduce();
        }

        public override void OnActionReceived(float[] vectorAction)
        {
            float horiz = vectorAction[0];
            float vert = vectorAction[1];
            float speed = vectorAction[2];
            float eat = vectorAction[3];
            //float reproduce = vectorAction[4];

            // Direction, normalized
            if (horiz == 2f)
                horiz = -1f;
            if (vert == 2f)
                vert = -1f;
            Vector3 dir = new Vector3(horiz, 0f, vert);
            dir = dir.normalized;

            // Speed, normalized
            speed = speed / 4f;

            // Eat
            if (eat == 1f)
            {
                // Try to eat
                // TODO: Return empty object if no food, and check if empty
                (GameObject, bool) food = FindFood();
                if (food.Item2)
                {
                    foodInstinct = food.Item1.transform.position;
                    creature.Eat(food.Item1);
                    AddReward(1.0f);
                }
            }

            creature.Move(dir, speed);
            
            if(creature.Energy <= 0)
            {
                AddReward(-1.0f);
                EndMe();
            }

            //// Try to reproduce
            //if(reproduce == 1f)
            //{
            //    bool child = creature.Reproduce();
            //    if (child)
            //    {
            //        AddReward(1.0f);
            //    }
            //}

            AddReward(-1f / 5000f);
        }

        // Reset the agent and area
        public override void OnEpisodeBegin()
        {
            creature.Energy = creature.MaxEnergy;
            transform.localPosition = GetRandomSpawnPoint();
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            // Add parameters

            // 1 value, INT
            sensor.AddObservation((int)creature.CreatureRegime);

            // 1 value, float
            float energyNorm = creature.Energy / creature.MaxEnergy;
            sensor.AddObservation(energyNorm);

            // 1 value, float
            sensor.AddObservation(creature.Size);

            // 3 values, vector3 
            var instinct = UseFoodInstinct();
            sensor.AddObservation(instinct.Item1);

            // 1 value, float
            sensor.AddObservation(instinct.Item2);


            (int[] forw_right, int[] forw_left, int[] back) = SenseSphere();

            // 12 values, int
            for (int i = 0; i < 4; i++)
            {
                sensor.AddObservation(forw_right[i]);
                sensor.AddObservation(forw_left[i]);
                sensor.AddObservation(back[i]);
            }

            // 19 VALUES TOTAL
        }

        public (int[], int[], int[]) SenseSphere()
        {
            var sphere = Physics.OverlapSphere(creature.transform.position, creature.Sensor.SensingRadius);

            //Debug.Log(Vector3.SignedAngle(transform.right, transform.forward, Vector3.up));
            //Debug.Log(Vector3.SignedAngle(transform.right*-1, transform.forward, Vector3.up));
            //Debug.Log(Vector3.SignedAngle(transform.forward*-1, transform.forward, Vector3.up));

            int[] forw_right = new int[4];
            int[] forw_left = new int[4];
            int[] back = new int[4];

            foreach (Collider collider in sphere)
            {
                var tag = collider.gameObject.tag;
                if (collider.gameObject == creature.gameObject)
                    continue;
                if (tag == "terrain")
                    continue;
                float angle = Vector3.SignedAngle(collider.transform.position - creature.transform.position, creature.transform.forward, Vector3.up);
                //Debug.Log(angle);
                if (angle < 90 && angle > -90)
                {
                    Debug.Log(tag + " " + " back");
                    switch (tag)
                    {
                        case "plant":
                            back[0] = 1;
                            break;
                        case "carnivore":
                            back[1] = 1;
                            break;
                        case "herbivore":
                            back[2] = 1;
                            break;
                        case "wall":
                            back[3] = 1;
                            break;
                        default:
                            break;
                    }
                }
                else if (angle > 90)
                {
                    Debug.Log(tag + " " + " right");
                    switch (tag)
                    {
                        case "plant":
                            forw_right[0] = 1;
                            break;
                        case "carnivore":
                            forw_right[1] = 1;
                            break;
                        case "herbivore":
                            forw_right[2] = 1;
                            break;
                        case "wall":
                            forw_right[3] = 1;
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    Debug.Log(tag + " " + " left");
                    switch (tag)
                    {
                        case "plant":
                            forw_left[0] = 1;
                            break;
                        case "carnivore":
                            forw_left[1] = 1;
                            break;
                        case "herbivore":
                            forw_left[2] = 1;
                            break;
                        case "wall":
                            forw_left[3] = 1;
                            break;
                        default:
                            break;
                    }
                }
            }

            return (forw_right, forw_left, back);
        }

        public void EndMe()
        {
            EndEpisode();
            //Destroy(creature);
        }

        public Vector3 GetRandomSpawnPoint()
        {
            float x_mean = spawnArea.x;
            float z_mean = spawnArea.z;
            float std = 10;

            System.Random rand = new System.Random();
            double x_u1 = 1.0f - rand.NextDouble();
            double x_u2 = 1.0f - rand.NextDouble();
            double x_randStdNormal = Math.Sqrt(-2.0 * Math.Log(x_u1)) * Math.Sin(2.0 * Math.PI * x_u2);

            double z_u1 = 1.0f - rand.NextDouble();
            double z_u2 = 1.0f - rand.NextDouble();
            double z_randStdNormal = Math.Sqrt(-2.0 * Math.Log(z_u1)) * Math.Sin(2.0 * Math.PI * z_u2);

            float x = x_mean + std * (float)x_randStdNormal;
            float z = z_mean + std * (float)z_randStdNormal;

            return new Vector3(x, 0.2f, z);

        }
    }
}