/* config.h.  Generated from config.h.in by configure.  */
/* config.h.in.  Generated from configure.ac by autoheader.  */

/* Define if building universal (internal helper macro) */
/* #undef AC_APPLE_UNIVERSAL_BUILD */

/* Define to 1 if you have the <dlfcn.h> header file. */
/* #undef HAVE_DLFCN_H */

/* Define to 1 if you have the <getopt.h> header file. */
#define HAVE_GETOPT_H 1

/* Define to 1 if you have the `getpwuid_r' function. */
/* #undef HAVE_GETPWUID_R */

/* Define to 1 if you have the `getrlimit' function. */
/* #undef HAVE_GETRLIMIT */

/* Define if you have the iconv() function and it works. */
/* #undef HAVE_ICONV */

/* Define to 1 if you have the <iconv.h> header file. */
/* #undef HAVE_ICONV_H */

/* Define to 1 if you have the <inttypes.h> header file. */
#define HAVE_INTTYPES_H 1

/* Define to 1 if you have the <langinfo.h> header file. */
/* #undef HAVE_LANGINFO_H */

/* Define to 1 if you have the <localcharset.h> header file. */
/* #undef HAVE_LOCALCHARSET_H */

/* Define to 1 if you have the <memory.h> header file. */
#define HAVE_MEMORY_H 1

/* Define to 1 if you have the <pwd.h> header file. */
/* #undef HAVE_PWD_H */

/* Define to 1 if you have the `rewinddir' function. */
#define HAVE_REWINDDIR 1

/* Define to 1 if you have the <stdint.h> header file. */
#define HAVE_STDINT_H 1

/* Define to 1 if you have the <stdlib.h> header file. */
#define HAVE_STDLIB_H 1

/* Define to 1 if you have the `stpcpy' function. */
/* #undef HAVE_STPCPY */

/* Define to 1 if you have the <strings.h> header file. */
#define HAVE_STRINGS_H 1

/* Define to 1 if you have the <string.h> header file. */
#define HAVE_STRING_H 1

/* Define to 1 if you have the `strlcpy' function. */
/* #undef HAVE_STRLCPY */

/* Define to 1 if you have the `strndup' function. */
/* #undef HAVE_STRNDUP */

/* Define to 1 if you have the `strtok_r' function. */
/* #undef HAVE_STRTOK_R */

/* Define to 1 if you have the <sys/resource.h> header file. */
/* #undef HAVE_SYS_RESOURCE_H */

/* Define to 1 if you have the <sys/select.h> header file. */
/* #undef HAVE_SYS_SELECT_H */

/* Define to 1 if you have the <sys/stat.h> header file. */
#define HAVE_SYS_STAT_H 1

/* Define to 1 if you have the <sys/time.h> header file. */
#define HAVE_SYS_TIME_H 1

/* Define to 1 if you have the <sys/types.h> header file. */
#define HAVE_SYS_TYPES_H 1

/* Define to 1 if you have the <sys/wait.h> header file. */
/* #undef HAVE_SYS_WAIT_H */

/* Define to 1 if you have the <unistd.h> header file. */
#define HAVE_UNISTD_H 1

/* Define to 1 if you have the `vasprintf' function. */
/* #undef HAVE_VASPRINTF */

/* Define as const if the declaration of iconv() needs const. */
/* #undef ICONV_CONST */

/* Define to the sub-directory where libtool stores uninstalled libraries. */
#define LT_OBJDIR ".libs/"

/* Name of package */
#define PACKAGE "eglib"

/* Define to the address where bug reports for this package should be sent. */
#define PACKAGE_BUGREPORT "http://bugzilla.xamarin.com/enter_bug.cgi?classification=Mono"

/* Define to the full name of this package. */
#define PACKAGE_NAME "eglib"

/* Define to the full name and version of this package. */
#define PACKAGE_STRING "eglib 0.3"

/* Define to the one symbol short name of this package. */
#define PACKAGE_TARNAME "eglib"

/* Define to the home page for this package. */
#define PACKAGE_URL ""

/* Define to the version of this package. */
#define PACKAGE_VERSION "0.3"

/* Targeting the Android platform */
/* #undef PLATFORM_ANDROID */

/* The size of `int', as computed by sizeof. */
#define SIZEOF_INT 4

/* The size of `long', as computed by sizeof. */
#define SIZEOF_LONG 4

/* The size of `long long', as computed by sizeof. */
#define SIZEOF_LONG_LONG 8

/* The size of `void *', as computed by sizeof. */
#define SIZEOF_VOID_P 4

/* Define to 1 if you have the ANSI C header files. */
#define STDC_HEADERS 1

/* The JIT/AOT targets iOS */
/* #undef TARGET_IOS */

/* The JIT/AOT targets Apple platforms */
/* #undef TARGET_MACH */

/* The JIT/AOT targets OSX */
/* #undef TARGET_OSX */

/* Version number of package */
#define VERSION "0.3"

/* Define WORDS_BIGENDIAN to 1 if your processor stores words with the most
   significant byte first (like Motorola and SPARC, unlike Intel). */
#if defined AC_APPLE_UNIVERSAL_BUILD
# if defined __BIG_ENDIAN__
#  define WORDS_BIGENDIAN 1
# endif
#else
# ifndef WORDS_BIGENDIAN
/* #  undef WORDS_BIGENDIAN */
# endif
#endif
