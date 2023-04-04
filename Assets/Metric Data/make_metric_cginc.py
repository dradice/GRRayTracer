#!/usr/bin/env python

import numpy as np
import scidata.xgraph as xg
import numpy as np

ahf = np.loadtxt("ahf.dat")
lapse = xg.parsefile("alpha.yg")
betar = xg.parsefile("betar.yg")
grr = xg.parsefile("ADMgrr.yg")
gT = xg.parsefile("ADMgT.yg")

lapse_frame = lapse.frame(lapse.nframes - 1)
betar_frame = betar.frame(betar.nframes - 1)
grr_frame = grr.frame(grr.nframes - 1)
gT_frame = gT.frame(gT.nframes - 1)

assert(len(lapse_frame.data_x) == len(betar_frame.data_x))
assert(len(lapse_frame.data_x) == len(grr_frame.data_x))
assert(len(lapse_frame.data_x) == len(gT_frame.data_x))
N = len(lapse_frame.data_y)

f = open("metric.cginc", "w")
f.write("#ifndef METRIC_CG_INCLUDE\n")
f.write("#define METRIC_CG_INCLUDE\n")
f.write("#define METRIC_SIZE {}\n\n".format(N))
f.write("static const float metric_horizon = {:e};\n".format(ahf[-1][1]))
f.write("static const float metric_rmin = {:e};\n".format(lapse_frame.data_x[0]))
f.write("static const float metric_dr = {:e};\n\n".format(np.mean(np.diff(lapse_frame.data_x))))

f.write("static const float metric_lapse[%s] = {\n" % N)
for i in range(N):
    f.write("    {:.15e},\n".format(lapse_frame.data_y[i]))
f.write("};\n\n")

f.write("static const float metric_betar[%s] = {\n" % N)
for i in range(N):
    f.write("    {:.15e},\n".format(betar_frame.data_y[i]))
f.write("};\n\n")

f.write("static const float metric_grr[%s] = {\n" % N)
for i in range(N):
    f.write("    {:.15e},\n".format(grr_frame.data_y[i]))
f.write("};\n\n")

f.write("static const float metric_gT[%s] = {\n" % N)
for i in range(N):
    f.write("    {:.15e},\n".format(gT_frame.data_y[i]))
f.write("};\n\n")

f.write("#endif\n")
