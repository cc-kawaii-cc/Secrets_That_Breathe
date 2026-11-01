"""Build the sparse cabin-lighting probes along FD's route."""
from pathlib import Path
import runpy
runpy.run_path(str(Path(__file__).with_name("BuildEveningSparseRideProbes.py")), run_name="__main__")
