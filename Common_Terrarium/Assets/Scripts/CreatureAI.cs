using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;

namespace Assets.Scripts
{
    public class CreatureAI : Agent
    {
        private Creature creature;
        public Vector3 spawnArea;

        public void Start()
        {
            Debug.Log($"Creature AI is ready");
            creature = GetComponent<Creature>();
            spawnArea = transform.localPosition;
        }

        private (GameObject, bool) FindFood()
        {
            List<GameObject> food = creature.Sensor.SensePlants(creature);
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
            if (creature.Energy > 0.2 * creature.MaxEnergy && UnityEngine.Random.Range(0,1)<0.1f) creature.Reproduce();
        }

        public override void OnActionReceived(float[] vectorAction)
        {
            float horiz = vectorAction[0];
            float vert = vectorAction[1];
            float speed = vectorAction[2];
            float eat = vectorAction[3];

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
                    creature.Eat(food.Item1);
                    AddReward(1.0f);
                    EndMe();
                }
            }

            creature.Move(dir, speed);
            //AddReward(-speed / 100f);

            AddReward(-1f / 3000f);
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
            sensor.AddObservation(creature.Energy);

            // 1 value, float
            sensor.AddObservation(creature.MaxEnergy);

            // 1 value, float
            sensor.AddObservation(creature.Size);

            (int[] forw_right, int[] forw_left, int[] back) = SenseSphere();

            for(int i = 0; i < 4; i++)
            {
                sensor.AddObservation(forw_right[i]);
                sensor.AddObservation(forw_left[i]);
                sensor.AddObservation(back[i]);
            }

            // 4 VALUES TOTAL
        }

        public (int[], int[], int[]) SenseSphere()
        {
            var sphere = Physics.OverlapSphere(transform.position, creature.Sensor.SensingRadius);

            int[] forw_right = new int[4];
            int[] forw_left = new int[4];
            int[] back = new int[4];

            foreach(Collider collider in sphere)
            {
                var tag = collider.gameObject.tag;
                float angle = Vector3.SignedAngle(transform.forward - transform.position, collider.transform.position - transform.position, Vector3.up);
                if (angle > 90 || angle < -90)
                {
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
                else if(angle <= 90 && angle >= 0)
                {
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