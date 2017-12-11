#!/bin/bash
FOLDER="Y:/proj/skuvault/dsl4/target"
TARGET="$(ls $FOLDER | grep uber)"
echo $TARGET

java -jar $FOLDER/$TARGET schema.clj
echo "java -jar $TARGET schema.clj" > update_schema.bat


