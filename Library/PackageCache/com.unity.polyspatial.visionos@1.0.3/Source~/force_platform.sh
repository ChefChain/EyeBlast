#!/bin/zsh

if [[ "$#" != 1 ]] ; then
    echo "Usage: $0 root_dir dest_sdk"
    echo "    root_dir is the directory inside which .a files are looked for"
    echo "    dest_sdk is the destination sdk, like xrsimulator or iphoneos"
    echo "       only used to know which .a files to skip (if they contain this string)"
    exit 1
fi

ROOT_DIR="$1"
SDK="$2"

#CACHE_DIR="~/.visionOS-hack-cache"
#mkdir -p ~/.visionOS-hack-cache

LIBFILES=( $(find $ROOT_DIR -name "*.a" -print) )

TEMPDIR=$(mktemp -d)
trap "rm -rf ${TEMPDIR}" EXIT

VPLATFORM="xros"
[[ "$SDK" == "xrsimulator" ]] && VPLATFORM=xrossim
[[ "$SDK" == "iphone" ]] && VPLATFORM=ios
[[ "$SDK" == "iphonesimulator" ]] && VPLATFORM=iossim

for libfile in $LIBFILES ; do
    if [[ "$libfile" =~ "$SDK" ]] ; then
        echo "Skipping $libfile..."
        continue
    fi

    echo "Processing $libfile..."
    libpath=$(realpath $ROOT_DIR/$libfile)

    pushd $TEMPDIR
    mkdir -p work
    pushd work

    ar x $libpath

    echo "Removing platforms..."
    find . -name "*.o" -print0 | xargs -0 -n 1 -I % \
        xcrun -sdk $SDK vtool -remove-build-version ios -remove-build-version iossim -remove-build-version xros -remove-build-version xrossim % -o %

    # xros load command is 24 bytes in size, ios is only 16. So we can't add it back;
    # just removing should be safe and allow the object everywhere.
    #echo "Adding $VPLATFORM..."
    #find . -name "*.o" -print0 | xargs -0 -n 1 -I % \
    #    xcrun -sdk $SDK vtool -set-build-version $VPLATFORM 1.0 1.0 % -o %

    echo "Rebuilding $libfile..."
    # real or symlink, doesn't matter. always replace with real.
    rm $libpath

    ar rcs $libpath *.o
    popd
    rm -rf work
    popd

done
