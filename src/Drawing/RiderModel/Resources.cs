﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace linerider.Drawing.RiderModel
{
    internal class Resources
    {
        protected Filenames Filenames = new Filenames();

        public List<string> RegionsCacheLines;
        public Bitmap Body { get; set; }
        public Bitmap BodyDead { get; set; }
        public Bitmap Sled { get; set; }
        public Bitmap SledBroken { get; set; }
        public Bitmap Arm { get; set; }
        public Bitmap Leg { get; set; }
        public Bitmap Rope { get; set; }
        public Bitmap Palette { get; set; }
        public Bitmap Regions { get; set; }
        public bool Legacy { get; protected set; }
        protected RegionsCache Cache { get; }

        public List<Rectangle> RegionsBody
        {
            get => Cache.RegionsBody;
        }
        public List<Rectangle> RegionsBodyDead
        {
            get => Cache.RegionsBodyDead;
        }
        public bool HasPalette
        {
            get => Palette != null;
        }
        public bool HasRegions
        {
            get => RegionsBody.Any() && RegionsBodyDead.Any();
        }
        public bool HasRope
        {
            get => Rope != null;
        }

        public Resources()
        {
            Cache = new RegionsCache();
            Cache.SetFilenames(Filenames);
        }

        public virtual void Load()
        {
            throw new NotImplementedException();
        }
    }
}
