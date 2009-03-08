/*
    base class for occupancy grids
    Copyright (C) 2009 Bob Mottram
    fuzzgun@gmail.com

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using sluggish.utilities;
using CenterSpace.Free;

namespace sentience.core
{
	public class occupancygridBase : pos3D
	{
        // lookup table for log odds calculations
        protected const int LOG_ODDS_LOOKUP_LEVELS = 1000;
        protected float[] LogOdds;
        
        // the number of cells across in the (xy) plane
        public int dimension_cells;

        // the number of cells in the vertical (z) axis
        public int dimension_cells_vertical;

        // size of each grid cell (voxel) in millimetres
        public int cellSize_mm;
        
        // when localising search a wider area than when mapping
        public int localisation_search_cells = 1;

        // the maximum range of features to insert into the grid
        protected int max_mapping_range_cells;
        
        // take some shortcuts to speed things up
        // this sacrifices some detail, but for most grid cell sizes is fine
        public bool TurboMode = true;
        
        // a quick lookup table for gaussian values
        protected float[] gaussianLookup;
        
						
	    #region "constructors"
	
		public occupancygridBase() : base(0, 0, 0)
		{
		}
		
		#endregion
		
        #region "setting the position of the grid"

        /// <summary>
        /// set the absolute position of the centre of the occupancy grid
        /// </summary>
        /// <param name="centre_x_mm">centre x position in millimetres</param>
        /// <param name="centre_y_mm">centre y position in millimetres</param>
        public void SetCentrePosition(float centre_x_mm, 
                                      float centre_y_mm)
        {
            x = centre_x_mm;
            y = centre_y_mm;
        }

        #endregion
	
        #region "sensor model"

        // a weight value used to define how aggressively the
        // carving out of space using the vacancy function works
        public float vacancy_weighting = 2.0f;

        protected float[] vacancy_model_lookup;

        /// <summary>
        /// creates a loouk table for the cacancy part of the ray model
        /// This avoids having to run slow Exp functions
        /// </summary>
        /// <param name="min_vacancy_probability"></param>
        /// <param name="max_vacancy_probability"></param>
        /// <param name="levels"></param>
        protected void CreateVacancyModelLookup(float min_vacancy_probability, 
                                              float max_vacancy_probability,
                                              int levels)
        {
            vacancy_model_lookup = new float[levels];
            for (int i = 0; i < levels; i++)
            {
                float fraction = i / (float)levels;
                vacancy_model_lookup[i] = 
                    vacancyFunction(fraction, levels, 
                                    min_vacancy_probability, max_vacancy_probability);
            }
        }

        /// <summary>
        /// function for vacancy within the sensor model
        /// </summary>
        /// <param name="fraction">fractional distance along the vacancy part of the ray model</param>
        /// <param name="steps"></param>
        /// <returns></returns>
        protected float vacancyFunction(float fraction, int steps,
                                      float min_vacancy_probability, 
                                      float max_vacancy_probability)
        {
            float prob = min_vacancy_probability + ((max_vacancy_probability - min_vacancy_probability) *
                         (float)Math.Exp(-(fraction * fraction)));
            return (prob);
        }

        /// <summary>
        /// vacancy part of the ray model
        /// </summary>
        /// <param name="fraction"></param>
        /// <param name="steps"></param>
        /// <returns></returns>
        protected float vacancyFunction(float fraction, int steps)
        {
            if (vacancy_model_lookup == null)
            {
                float min_vacancy_probability = 0.1f;
                float max_vacancy_probability = vacancy_weighting;
                CreateVacancyModelLookup(min_vacancy_probability, max_vacancy_probability, 1000);
            }
            float prob = vacancy_model_lookup[(int)(fraction * (vacancy_model_lookup.Length-1))];
            return (0.5f - (prob / steps));
        }

        #endregion	
					
					
        #region "calculating the matching probability"

        /// <summary>
        /// returns a measure of the difference between two colours
        /// </summary>
        /// <param name="colour1">the first colour</param>
        /// <param name="colour2">the second colour</param>
        /// <returns>difference between the two colours</returns>
        protected float getColourDifference(byte[] colour1, float[] colour2)
        {
            // note that relative colour values are used, since comparing absolute RGB
            // values is a road to nowhere
            float colour_difference = 0;
            for (int col = 0; col < 3; col++)
            {
                int col2 = col + 1;
                if (col2 > 2) col2 -= 3;
                int col3 = col + 2;
                if (col3 > 2) col3 -= 3;

                float c1 = (colour1[col] * 2) - colour1[col2] - colour1[col3];
                if (c1 < 0) c1 = 0;

                float c2 = (int)((colour2[col] * 2) - colour2[col2] - colour2[col3]);
                if (c2 < 0) c2 = 0;

                colour_difference += Math.Abs(c1 - c2);
            }
            //colour_difference /= (6 * 255.0f);
            colour_difference *= 0.0006535947712418f;

            return (colour_difference);
        }
        
        #endregion
					
	}
}
