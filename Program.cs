using InfiniteForgeTool.code_stuff;
using System.Collections.Generic;

namespace forgle_colors{
    internal class Program{
        static void Main(string[] args){
            forge_colors_processor process = new forge_colors_processor();
            process.begin();
    }}

    public class forge_colors_processor{

        const string file_output = "C:\\Users\\Joe bingle\\Downloads\\IFIMT research\\colors.txt";

        const string target_process = "HaloInfinite";
        const string target_module  = "HaloInfinite.exe";

        // 6/29/2023 AU : tag_instance address : HaloInfinite.exe+0x487ADE8
        const long target_offset = 0x487ADE8;

        const long color_tag_block_length = 0x10;




        CMem mem = new CMem();
        public void begin(){

            if (!mem.hook_and_open_process(target_process)){
                Console.WriteLine("failed to find halo infinite process");
                return;
            }

            long? tags_ptr = mem.read_module_pointer(target_module, target_offset);
            if (tags_ptr == null){
                Console.WriteLine("failed to find tag instance ptr");
                return;
            }

            long? color_tag_block_ptr = find_color_address((long)tags_ptr);
            if (color_tag_block_ptr == null){
                Console.WriteLine("failed to find color tag block ptr");
                return;
            }

            List <calor>? color_list = fetch_all_colors_from_tag((long)color_tag_block_ptr);
            if (color_list == null){
                Console.WriteLine("failed to find color information");
                return;
            }

            Console.WriteLine("beginning write");
            // begin writing the output into a copy pastable structure lol
            using (var fs = new FileStream(file_output, FileMode.Append))
            using (var sw = new StreamWriter(fs)){

                sw.WriteLine("float[,] list = {");
                foreach (calor cal in color_list)
                    sw.WriteLine("    {"+ cal.red + "," + cal.green + "," + cal.blue + "},");
                sw.WriteLine("};");

            }
            Console.WriteLine("write completed");

        }

        long? find_color_address(long tags_ptr){

            long? TagCount = mem.read_int32(tags_ptr + 0x6CL);
            if (TagCount == null) return null;

            long? tagsStart = mem.read_int64(tags_ptr + 0x78L);
            if (tagsStart == null) return null;

            for (int tagIndex = 0; tagIndex < TagCount; tagIndex++){
                long tagAddress = (long)tagsStart + (tagIndex * 52);
                long? TagID = mem.read_int32(tagAddress + 4);
                if (TagID == null) continue;

                if (TagID == 0x00000051){
                    long? actualTagAddress = mem.read_int64(tagAddress + 0x10);
                    if (actualTagAddress == null) return null;
                    return actualTagAddress;
            }}
            return -1;
        }

        struct calor{
            public calor(float _red, float _greem, float _blue){
                red = _red;
                green = _greem;
                blue = _blue;
            }
            public float red;
            public float green;
            public float blue;
        }

        List<calor>? fetch_all_colors_from_tag(long tag_address){
            int? colors_count = mem.read_int32(tag_address + 0x20);
            if (colors_count == null) return null;

            long? data_block_start = mem.read_int64(tag_address + 0x10);
            if (data_block_start == null) return null;

            List<calor> color_list = new();

            for (int i = 0; i < colors_count; i++){
                long color_address = (long)data_block_start + (i * color_tag_block_length);

                float? red   = mem.read_float(color_address + 0x4);
                if (red == null)   continue;
                float? green = mem.read_float(color_address + 0x8);
                if (green == null) continue;
                float? blue  = mem.read_float(color_address + 0xC);
                if (blue == null)  continue;

                color_list.Add(new calor((float)red, (float)green, (float)blue));
            }
            return color_list;
        }


    }
}