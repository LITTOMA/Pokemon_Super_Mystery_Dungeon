from struct import unpack as bin_unpack,pack as bin_pack
import os,binascii,sys

def mkdir(path):
    if not os.path.exists(path):
        os.makedirs(path)

class entry:
    def __init__(self,data):
        self.filename = ''
        self.filename_hash, self.file_offset, self.file_size = bin_unpack('<III',data[:12])

def read_entries(filename):
    with open(filename,'rb')as farc:
        farc.seek(0x24,0)
        hashtable_offset,hashtable_size,filedata_offset,data_size = bin_unpack('<IIII',farc.read(16))
        farc.seek(hashtable_offset,0)
        if not farc.read(4)=='SIR0':
            return False
        
        pFnte,pPchtbls = bin_unpack('<II',farc.read(8))
        farc.seek(hashtable_offset+pFnte,0)
        fntable_end,file_count = bin_unpack('<II',farc.read(8))

        entries = []
        farc.seek(hashtable_offset+0x10,0)
        if fntable_end == 0x10:
            for i in range(file_count):
                data = farc.read(12)
                entries.append(entry(data))
            fnlist = os.path.splitext(filename)[0]+'.lst'
            if os.path.exists(fnlist):
                with open(fnlist,'rb')as fnlist:
                    fnlist = fnlist.readlines()
                for i in range(len(fnlist)):
                    fn = os.path.split(fnlist[i])[1].replace('\n','')
                    fnu = fn.encode('utf16')[2:]
                    fnh = binascii.crc32(fnu)&0xffffffff
                    for j in range(len(entries)):
                        if entries[j].filename_hash == fnh:
                            entries[j].filename = fn
                            break
            i = 0
            j = 0
            while i<fntable_end:
                c = farc.read(2)
                temp = ''
                while not c == '\x00\x00':
                    temp += c
                    c = farc.read(2)
                    i += 2
                if not temp == '':
                    fnh = binascii.crc32(temp)&0xffffffff
                    for x in range(len(entries)):
                        if entries[x].filename_hash == fnh:
                            entries[x].filename = temp
                temp = ''
                j+=1
    return entries

def unpack_file(filename):
    entries = read_entries(filename)
    with open(filename,'rb')as farc:
        farc.seek(0x2C)
        data_offset = bin_unpack('<I',farc.read(4))[0]
        for e in entries:
            filedir = os.path.splitext(filename)[0]
            filepath = filedir+'\\'+e.filename
            mkdir(filedir)
            with open(filepath,'wb')as out:
                print 'Saving:',filepath
                farc.seek(data_offset+e.file_offset,0)
                out.write(farc.read(e.file_size))

unpack_file(sys.argv[1])
