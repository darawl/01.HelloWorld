using CCT.NUI.Core;
using CCT.NUI.HandTracking;
using CCT.NUI.KinectSDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace _01.HelloWorld
{
    class Kinect
    {
        const double pinchFingerDist = 0.003d;
        const double openHandDist = 0.0075d;
        //const double closedHandInterval = .0015d;
        const double angleRotationThreshhold = .196d;
        const double translationThreshhold = 200;
        const double extractionThreshold = 2.0d;

        bool leftFistRightOpen = false;
        bool isRotating = false;
        bool isTranslating = false;
        double initAngle = 0;
        Point initPos;


        //Variables public for Irrlicht reading:
        public double rotation = 0;
        public Point translation;
        public Point position;

        public Kinect()
        {
            IDataSourceFactory dataSourceFactory = new SDKDataSourceFactory();
            var handDataSource = new HandDataSource(dataSourceFactory.CreateShapeDataSource());

            handDataSource.NewDataAvailable += new NewDataHandler<HandCollection>(handDataSource_NewDataAvailable);
            handDataSource.Start();
        }

        private double getDistance(Point loc1, Point loc2)
        {
            return Math.Sqrt((loc1.X - loc2.X) * (loc1.X - loc2.X) + (loc1.Y - loc2.Y) * (loc1.Y - loc2.Y) + (loc1.Z - loc2.Z) * (loc1.Z - loc2.Z));
        }

        private double getDistanceXY(Point loc1, Point loc2)
        {
            return Math.Sqrt((loc1.X - loc2.X) * (loc1.X - loc2.X) + (loc1.Y - loc2.Y) * (loc1.Y - loc2.Y));
        }

        /*private double getLargestFingerDist(IList<FingerPoint> fingers)
        {
            int leftMost = 0, rightMost = 1;

            for (int i = 2; i < fingers.Count; i++)
            {
                if(getDistance(fingers[leftMost].Location, fingers[i].Location) > getDistance(fingers[leftMost].Location, fingers[rightMost].Location)
                {
                    if(getDistance(fingers[leftMost].Location, fingers[i].Location) < getDistance(fingers[rightMost].Location, fingers[i].Location))
                    {
                        leftMost = i;
                    }
                    else
                    {
                        rightMost = i;
                    }
                }

                else if(getDistance(fingers[rightMost].Location, fingers[i].Location) > getDistance(fingers[leftMost].Location, fingers[rightMost].Location))
                {
                    leftMost = i;
                }
            }

            return getDistance(fingers[leftMost].Location, fingers[rightMost].Location);
        }*/

        private bool isHandPinched(HandData hand)
        {
            //Console.WriteLine(hand.FingerCount);
            for (int i = 0; i < hand.FingerCount; i++)
            {
                //Console.WriteLine(getDistanceXY(hand.Fingers.ElementAt(i).Fingertip, hand.Location));
                if (getDistanceXY(hand.Fingers.ElementAt(i).Fingertip, hand.Location) > pinchFingerDist)
                {
                    return false;
                }
            }
            return true;
        }

        private bool isHandOpened(HandData hand)
        {
            bool onceIsOkay = true;
            for (int i = 0; i < hand.FingerCount; i++)
            {
                if (getDistance(hand.Fingers.ElementAt(i).Fingertip, hand.Location) < openHandDist)
                {
                    if (onceIsOkay)
                    {
                        onceIsOkay = false;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /*private bool isClosedFist(IList<FingerPoint> fingers)
        {
            return true;
            for (int i = 2; i < fingers.Count; i++)
            {
                
            }
        }*/
        private double averageHandAngle(HandData hand)
        {
            double totalAngle = 0;
            for (int i = 0; i < hand.FingerCount; i++)
            {
                if (hand.FingerPoints[i].Location.X == hand.Location.X)
                {
                    totalAngle += Math.PI / 2;
                }
                else
                {
                    totalAngle += (hand.FingerPoints[i].Location.X < hand.Location.X) ? Math.PI : 0;
                    totalAngle += Math.Tan((hand.FingerPoints[i].Location.Y - hand.Location.Y) / (hand.FingerPoints[i].Location.X - hand.Location.X));
                }
            }
            totalAngle /= hand.FingerCount;

            return totalAngle;
        }
        private void handDataSource_NewDataAvailable(HandCollection data)
        {
            int left = 0;
            int right = 1;
            if (data.Hands.Count < 2)
            {
                return;
            }


            if (data.Hands[right].Location.Z > data.Hands[left].Location.Z)
            {
                left = 1; right = 0;
            }

            for (int i = 2; i < data.Hands.Count; i++)
            {
                if (data.Hands[i].Location.Z < data.Hands[left].Location.Z)
                {
                    if (data.Hands[right].Location.Z > data.Hands[i].Location.Z)
                    {
                        left = right;
                        right = i;
                    }

                    else
                    {
                        left = i;
                    }
                }
            }

            if (data.Hands[right].Location.X < data.Hands[left].Location.X)
            {
                int temp = left;
                left = right;
                right = temp;
            }

            //double fingerDist = getLargestFingerDist(data.Hands[left].FingerPoints);

            Console.WriteLine(data.Hands[left].FingerCount + ", " + data.Hands[right].FingerCount);
            if (leftFistRightOpen)
            {
                if (!isTranslating && !isRotating && Math.Abs(initAngle - averageHandAngle(data.Hands[right])) > angleRotationThreshhold)
                {
                    isRotating = true;
                    initAngle += averageHandAngle(data.Hands[right]);
                    isTranslating = false;
                }
                else if (!isRotating && !isTranslating && getDistanceXY(initPos, data.Hands[right].Location) > translationThreshhold)
                {
                    isRotating = false;
                    initPos = data.Hands[right].Location;
                    isTranslating = true;
                }
                else if (isRotating)
                {
                    rotation = averageHandAngle(data.Hands[right]) - initAngle;
                    Console.WriteLine("Rotating: " + rotation);
                }
                else if (isTranslating)
                {
                    translation.X = initPos.X - data.Hands[right].Location.X;
                    translation.Y = initPos.Y - data.Hands[right].Location.Y;
                    translation.Z = initPos.Z - data.Hands[right].Location.Z;
                    Console.WriteLine("Translating. X: " + translation.X + ", Y: " + translation.Y + ", Z: " + translation.Z);
                }
                position = data.Hands[right].Location;
            }
            if (data.Hands[left].FingerCount == 0 && data.Hands[right].FingerCount == 5)
            {
                if (!leftFistRightOpen)
                {
                    Console.WriteLine("leftFistRightOpen is being set.");
                    leftFistRightOpen = true;
                    initAngle = averageHandAngle(data.Hands[right]);
                    initPos = data.Hands[left].Location;
                }
            }
            else if (data.Hands[left].FingerCount != 0 || data.Hands[right].FingerCount < 3)
            {
                leftFistRightOpen = false;
                isRotating = false;
                isTranslating = false;
                if (data.Hands[right].FingerCount == 0)
                {
                    /*if (data.Hands[right].Location.Z < initPos.Z)
                    {
                        HandZtemp = data.Hands[right].Location.Z;
                    }

                    if (data.Hands[right].Location.Z > HandZtemp)
                    {
                        HandZtemp = data.Hands[right].Location.Z;
                    }*/
                }
            }

            /*if (isHandPinched(data.Hands[right]))
            {
                //Console.WriteLine("Hand is Pinched");
            }*/

            /*if (isHandOpened(data.Hands[left]) || isHandOpened(data.Hands[right]))
            {
                //Console.WriteLine("Hand is Opened");
            }*/

        }
    }
}
