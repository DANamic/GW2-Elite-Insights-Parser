﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuckParser.Models.ParseModels
{
    public class CombatReplay
    {
        private List<Point3D> positions = new List<Point3D>();
        private List<Point3D> velocities = new List<Point3D>();
        private long start = 0;
        private long end = 0;
        // icon
        private string icon;
        //status
        private List<Tuple<long, long>> dead = new List<Tuple<long, long>>();
        private List<Tuple<long, long>> down = new List<Tuple<long, long>>();
        private List<Tuple<long, long>> dc = new List<Tuple<long, long>>();
        // dps
        private List<int> dps = new List<int>();
        private List<int> dps10s = new List<int>();
        private List<int> dps30s = new List<int>();
        // boons
        private Dictionary<long, List<int>> boons = new Dictionary<long, List<int>>();
        // actors
        private List<CircleActor> circleActors = new List<CircleActor>();
        private List<DoughnutActor> doughnutActors = new List<DoughnutActor>();

        public CombatReplay()
        {

        }

        public void setIcon(string icon)
        {
            this.icon = icon;
        }

        public void addPosition(Point3D pos)
        {
            positions.Add(pos);
        }

        public void addVelocity(Point3D vel)
        {
            velocities.Add(vel);
        }

        public Tuple<long, long> getTimeOffsets()
        {
            return new Tuple<long, long>(start, end);
        }

        public void addDPS(int dps)
        {
            this.dps.Add(dps);
        }
        public void addDPS10s(int dps)
        {
            dps10s.Add(dps);
        }
        public void addDPS30s(int dps)
        {
            dps30s.Add(dps);
        }
        public void addCircleActor(CircleActor circleActor)
        {
            circleActors.Add(circleActor);
        }
        public void addDoughnutActor(DoughnutActor doughnutActor)
        {
            doughnutActors.Add(doughnutActor);
        }

        public void setStatus(List<Tuple<long, long>> down, List<Tuple<long, long>> dead, List<Tuple<long, long>> dc)
        {
            this.down = down;
            this.dead = dead;
            this.dc = dc;
        }

        public void trim(long start, long end)
        {
            this.start = start;
            this.end = end;
            positions.RemoveAll(x => x.time < start || x.time > end);
            if (positions.Count == 0)
            {
                this.start = -1;
                this.end = -1;
            }
        }

        public List<CircleActor> getCircleActors()
        {
            return circleActors;
        }

        public List<DoughnutActor> getDoughnutActors()
        {
            return doughnutActors;
        }

        public List<Tuple<long, long>> getDead()
        {
            return dead;
        }

        public List<Tuple<long, long>> getDown()
        {
            return down;
        }

        public List<Tuple<long, long>> getDC()
        {
            return dc;
        }

        public void addBoon(long id, int value)
        {
            List<int> ll;
            if (!boons.TryGetValue(id, out ll))
            {
                ll = new List<int>();
                boons.Add(id, ll);
            }
            ll.Add(value);
        }

        public List<int> getTimes()
        {
            return positions.Select(x => (int)x.time).ToList();
        }

        public string getIcon()
        {
            return icon;
        }

        public void pollingRate(int rate, long fightDuration, bool forceInterpolate)
        {
            if (positions.Count == 0)
            {
                start = -1;
                end = -1;
                return;
            }
            else if (positions.Count == 1 && !forceInterpolate)
            {
                velocities = null;
                return;
            }
            List<Point3D> interpolatedPositions = new List<Point3D>();
            int tablePos = 0;
            Point3D currentVelocity = null;
            for (int i = -1000; i < fightDuration; i += rate)
            {
                Point3D pt = positions[tablePos];
                if (i <= pt.time)
                {
                    currentVelocity = null;
                    interpolatedPositions.Add(new Point3D(pt.X, pt.Y, pt.Z, i));
                }
                else
                {
                    if (tablePos == positions.Count - 1)
                    {
                        interpolatedPositions.Add(new Point3D(pt.X, pt.Y, pt.Z, i));
                    }
                    else
                    {
                        Point3D ptn = positions[tablePos + 1];
                        if (ptn.time < i)
                        {
                            tablePos++;
                            currentVelocity = null;
                            interpolatedPositions.Add(new Point3D(ptn.X, ptn.Y, ptn.Z, i));
                        }
                        else
                        {
                            Point3D last = interpolatedPositions.Last();
                            Point3D velocity = velocities.Find(x => x.time <= i && x.time > last.time);
                            currentVelocity = velocity != null ? velocity : currentVelocity;
                            if (ptn.time - pt.time < 400)
                            {
                                float ratio = (float)(i - pt.time) / (ptn.time - pt.time);
                                interpolatedPositions.Add(new Point3D(pt, ptn, ratio, i));
                            }
                            else
                            {
                                if (currentVelocity == null || (Math.Abs(currentVelocity.X) <= 1e-1 && Math.Abs(currentVelocity.Y) <= 1e-1))
                                {
                                    interpolatedPositions.Add(new Point3D(last.X, last.Y, last.Z, i));
                                }
                                else
                                {
                                    float ratio = (float)(i - last.time) / (ptn.time - last.time);
                                    interpolatedPositions.Add(new Point3D(last, ptn, ratio, i));
                                }
                            }

                        }
                    }
                }
            }
            positions = interpolatedPositions.Where(x => x.time >= 0).ToList();
            velocities = null;
        }

        public List<Point3D> getPositions()
        {
            return positions;
        }

        public List<Point3D> getActivePositions()
        {
            List<Point3D> activePositions = new List<Point3D>(positions);
            for (var i = 0; i < activePositions.Count; i++)
            {
                Point3D cur = activePositions[i];
                foreach (Tuple<long, long> status in dead)
                {
                    if (cur.time >= status.Item1 && cur.time <= status.Item2)
                    {
                        activePositions[i] = null;
                    }
                }
                foreach (Tuple<long, long> status in dc)
                {
                    if (cur.time >= status.Item1 && cur.time <= status.Item2)
                    {
                        activePositions[i] = null;
                    }
                }
            }        
            return activePositions;
        }
    }
}