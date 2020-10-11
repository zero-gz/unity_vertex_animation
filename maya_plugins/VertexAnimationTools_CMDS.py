import maya.cmds as cm
import math
from functools import partial


# Helper Functions
def f(vector):
    return [float("{:.15f}".format(c)) for c in vector]

def clamp(value, min, max):
    return min if value < min else max if value > max else value


def clamp_v(vector, min, max):
    return [clamp(component, min, max) for component in vector]


def abs_v(vector):
    return [abs(component) for component in vector]


def normalize(vector):
    magnitude = math.sqrt((vector[0] * vector[0]) + (vector[1] * vector[1]) + (vector[2] * vector[2]))
    if magnitude == 0:
        return vector
    else:
        return [vector[0] / magnitude, vector[1] / magnitude, vector[2] / magnitude]


def sub(vec_a, vec_b):
    return [vec_a[0] - vec_b[0], vec_a[1] - vec_b[1], vec_a[2] - vec_b[2]]


def scale(vector, scalar):
    return [vector[0] * scalar, vector[1] * scalar, vector[2] * scalar]


# System Functions

class Undo(object):
    """
    Undo Context Helper
    With Undo():
        Single Undo Chunk
    With Undo(0):
        Omit from Undo
    """
    def __init__(self, result=1):
        self.result = result

    def __enter__(self):
        if self.result == 0:
            cm.undoInfo(stateWithoutFlush=0)
        else:
            cm.undoInfo(openChunk=1)

    def __exit__(self, *exc_info):
        if self.result == 0:
            cm.undoInfo(stateWithoutFlush=1)
        else:
            cm.undoInfo(closeChunk=1)


class rpartial(partial):
    """
    Last argument passed to rpartial used for Undo / Redo print result
    """
    def __init__(self, *args):
        self.result = args[-1]

    def __repr__(self):
        return self.result
